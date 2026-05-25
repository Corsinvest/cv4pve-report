/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Storage;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>Report engine for Proxmox VE infrastructure.</summary>
public partial class ReportEngine(PveClient client, Settings settings, ReportInfo info)
{
    /// <summary>Optional provider to calculate snapshot size. Parameters: node, vmType, vmId, snapName. Returns size in bytes or null if unavailable.</summary>
    public Func<string, VmType, long, string, Task<long>>? SnapshotSizeProvider { get; set; }

    /// <summary>SVG of the network diagram produced by the last <see cref="GenerateAsync"/> call. Null if no report has been generated yet.</summary>
    public string? NetworkDiagramSvg { get; private set; }

    private IProgress<ReportProgress>? _progress;
    private List<ClusterResource> _resources = [];
    private Dictionary<long, ClusterResource> _resourcesByVmId = [];
    private List<ClusterResource> _uniqueStorages = [];
    private readonly List<VmNetworkRow> _pendingNetworkRows = [];
    private readonly List<(string Node, NodeNetwork Network)> _pendingNodeNetworkRows = [];
    private IEnumerable<StorageItem> _storageConfigs = [];
    private IReadOnlyList<ClusterSdnVnet> _sdnVnets = [];
    private IReadOnlyList<ClusterSdnZone> _sdnZones = [];
    private IReadOnlyList<NetworkDiagramBuilder.SdnVnetRow> _sdnRows = [];
    private readonly List<(ClusterResource Vm, IEnumerable<VmDisk> Disks)> _pendingDiskRows = [];
    private readonly List<(ClusterResource Vm, IEnumerable<VmQemuAgentGetFsInfo.ResultInfo> Partitions)> _pendingPartitionRows = [];
    private HashSet<long> _vmIds = [];
    private readonly IssueTracker _issues = new();
    private IReportWriter _writer = null!;

    private record VmNetworkRow(long VmId,
                                string Name,
                                string Node,
                                string Type,
                                string Status,
                                string Hostname,
                                VmNetwork Network,
                                bool IsInternal);

    private IEnumerable<ClusterResource> GetResources(ClusterResourceType type)
        => type switch
        {
            ClusterResourceType.Node => _resources.Where(a => a.ResourceType == ClusterResourceType.Node
                                                           && CheckNames(settings.Node.Names, a.Node)),
            ClusterResourceType.Vm => _resources.Where(a => a.ResourceType == ClusterResourceType.Vm
                                                          && _vmIds.Contains(a.VmId)),
            ClusterResourceType.Storage => _resources.Where(a => a.ResourceType == ClusterResourceType.Storage),
            _ => _resources.Where(a => a.ResourceType == type),
        };

    private async Task LoadResourcesAsync()
    {
        _resources = [.. await client.GetResourcesAsync(ClusterResourceType.All)];
        _resources.CalculateHostUsage();

        _resourcesByVmId = _resources.Where(r => r.VmId > 0)
                                     .GroupBy(r => r.VmId)
                                     .ToDictionary(g => g.Key, g => g.First());

        _uniqueStorages = [.. _resources.Where(a => a.ResourceType == ClusterResourceType.Storage)
                                        .GroupBy(a => a.Shared
                                                        ? $"shared:{a.Storage}"
                                                        : $"{a.Node}:{a.Storage}")
                                        .Select(g => g.First())];

        _vmIds = [.. (await client.GetVmsAsync(settings.Guest.Ids)).Select(a => a.VmId)];

        // Cluster-wide bootstrap data, consumed by multiple sections (Cluster sheet, Network
        // sheet) and the SVG diagram. Loaded once in parallel.
        var storageTask = client.Storage.GetAsync().ToSafeEnum(_issues, "Cluster", LinkKey.Cluster);
        var sdnVnetsTask = client.Cluster.Sdn.Vnets.GetAsync().ToSafeEnum(_issues, "Cluster", LinkKey.Cluster);
        var sdnZonesTask = client.Cluster.Sdn.Zones.GetAsync().ToSafeEnum(_issues, "Cluster", LinkKey.Cluster);
        await Task.WhenAll(storageTask, sdnVnetsTask, sdnZonesTask);

        _storageConfigs = storageTask.Result;
        _sdnVnets = sdnVnetsTask.Result;
        _sdnZones = sdnZonesTask.Result;
        _sdnRows = [.. _sdnVnets.Select(v =>
        {
            var zone = _sdnZones.FirstOrDefault(z => z.Zone == v.Zone);
            var nodes = string.IsNullOrEmpty(zone?.Nodes)
                            ? (IReadOnlyList<string>)[.. _resources.Where(r => r.ResourceType == ClusterResourceType.Node).Select(r => r.Node)]
                            : [.. zone.Nodes.Split(',').Select(s => s.Trim())];
            return new NetworkDiagramBuilder.SdnVnetRow(
                Vnet: v.Vnet ?? "",
                Zone: v.Zone ?? "",
                ZoneType: zone?.Type ?? "simple",
                ZoneBridge: zone?.Bridge,
                Tag: v.Tag,
                Alias: v.Alias,
                Nodes: nodes);
        })];

        _writer.Links[LinkKey.Storages] = "Storages";
        _writer.Links[LinkKey.ListNodes] = "Nodes";
        _writer.Links[LinkKey.ListVms] = "VMs";
        _writer.Links[LinkKey.ListContainers] = "Containers";

        foreach (var item in _resources)
        {
            switch (item.ResourceType)
            {
                case ClusterResourceType.Node:
                    _writer.Links[LinkKey.Node(item.Node)] = $"Node {item.Node}";
                    break;

                case ClusterResourceType.Vm:
                    _writer.Links[LinkKey.Vm(item.VmId)] = $"{VmTypeLabel(item.VmType)} {item.VmId}";
                    break;
            }
        }
    }

    /// <summary>Generates the report in the requested format and returns it as a stream.</summary>
    public async Task<Stream> GenerateAsync(ReportFormat format,
                                            IProgress<ReportProgress>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(info);

        _progress = progress;
        var originalTimeout = client.Timeout;
        if (settings.ApiTimeout > 0) { client.Timeout = TimeSpan.FromSeconds(settings.ApiTimeout); }
        var stream = new MemoryStream();
        try
        {
            await WriteReportAsync(format, stream);
        }
        finally
        {
            client.Timeout = originalTimeout;
        }
        stream.Position = 0;
        return stream;
    }

    private void ReportGlobal(string step)
        => _progress?.Report(new ReportProgress { Step = step });

    private async Task WriteReportAsync(ReportFormat format, Stream stream)
    {
        ReportGlobal("Init");
        _writer = format switch
        {
            ReportFormat.Xlsx => new Writers.Xlsx.XlsxReportWriter(info),
            ReportFormat.Html => new Writers.Html.HtmlReportWriter(info),
            ReportFormat.Json => new Writers.Json.JsonReportWriter(info),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
        };

        await LoadResourcesAsync();

        var sections = new Section[]
        {
            new("Cluster", "Cluster overview, users, roles, ACL, firewall, backup jobs", AddClusterDataAsync),
            new("Storages", "Storage list with size, usage and type", AddStoragesDataAsync),
            new("Nodes", "Node list with hardware, subscription, DNS, kernel details", AddNodesDataAsync),
            new("VMs", "Virtual machines (QEMU) with agent info, OS name/version/kernel, bios, cpu, memory and disk details", AddVmsDataAsync),
            new("Containers", "LXC containers with hostname, swap, nameserver and privilege details", AddContainersDataAsync),
            new("Network", "Global network overview: node interfaces and VM/CT network inventory", () => Task.FromResult(WriteNetworkData())),
            new("Storage Content", "Storage content inventory (ISO, templates, disk images — excludes backups)", AddStorageContentDataAsync),
            new("Disks", "Global disk inventory: VM/CT disk configuration", () => Task.FromResult(WriteDiskData())),
            new("Partitions", "Guest filesystem partitions with used/total space from QEMU agent", () => Task.FromResult(WritePartitionData())),
            new("Snapshots", "Global snapshot inventory across all VMs and containers", AddSnapshotsDataAsync),
            new("Firewall", "Global firewall rules, aliases and IPSets across cluster, nodes, VMs and containers", AddFirewallDataAsync),
            new("Replication", "Global replication status across all nodes: last sync, next sync, errors and duration", AddReplicationDataAsync),
            new("RRD Nodes", "Historical performance data (CPU, memory, swap, disk, network) for all nodes", AddRrdNodeDataAsync),
            new("RRD Storage", "Historical performance data (size, used, usage%) for all storages", AddRrdStorageDataAsync),
            new("RRD Guests", "Historical performance data (CPU, memory, disk, network) for all VMs and containers", AddRrdGuestDataAsync),
            new("Syslog", "Systemd journal per node parsed into date, time, host, service, pid and message", AddSyslogDataAsync),
            new("Cluster Log", "Cluster log with user, node, service and message", AddClusterLogDataAsync),
            new("Cluster Tasks", "All recent tasks across the cluster with status, duration and node", AddClusterTasksDataAsync),
        };

        var stats = new List<SectionStat>();
        var sw = Stopwatch.StartNew();
        foreach (var s in sections)
        {
            ReportGlobal(s.Name);
            sw.Restart();
            stats.Add(new(s.Name, s.Description, await s.Action(), sw.Elapsed));
        }

        // Issues is generated last (so it sees every section's failures) and surfaced as the
        // second cover entry (right after Cluster) when non-empty.
        AddIssuesSection(stats, sw);

        ReportGlobal("Network Diagram");
        BuildAndStoreNetworkDiagramSvg();
        if (NetworkDiagramSvg is { Length: > 0 } svg) { _writer.SetNetworkDiagram(svg); }

        ReportGlobal("Cover");
        _writer.WriteCoverPage(settings, stats);

        ReportGlobal("Saving");
        await _writer.SaveAsync(stream);

        _writer.Dispose();
    }

    private static string StorageNode(ClusterResource item)
        => item.Shared
            ? "(shared)"
            : item.Node;

    internal static string VmTypeLabel(VmType type)
        => type == VmType.Qemu
            ? "VM"
            : "CT";

    private Task<TResult[]> RunParallelAsync<T, TResult>(IEnumerable<T> source, Func<T, Task<TResult>> func)
    {
        var semaphore = new SemaphoreSlim(settings.MaxParallelRequests);
        return Task.WhenAll(source.Select(async item =>
        {
            await semaphore.WaitAsync();
            try { return await func(item); }
            finally { semaphore.Release(); }
        }));
    }

    private static string ToX(bool value)
        => value
            ? "X"
            : "";

    private static string ToX(bool? value)
        => value is true
            ? "X"
            : "";

    private static bool? TrueOrNull(bool value)
        => value
            ? true
            : null;

    private static int? IntOrNull(int value)
        => value > 0
            ? value
            : null;

    private static string ToNewLine(string? value, string character = ",")
        => value?.Replace(character, Environment.NewLine) ?? "";

    private static DateTime? FromUnixTime(long seconds)
        => seconds == 0
            ? null
            : DateTimeOffset.FromUnixTimeSeconds(seconds).DateTime;

    private static readonly ConcurrentDictionary<string, Regex> _checkNamesRegexCache = new();

    private static bool CheckNames(string names, string name)
    {
        if (string.IsNullOrWhiteSpace(names) || names.Equals("@all", StringComparison.OrdinalIgnoreCase)) { return true; }

        foreach (var token in names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!token.Contains('*'))
            {
                if (token.Equals(name, StringComparison.OrdinalIgnoreCase)) { return true; }
            }
            else
            {
                if (token == "*") { return true; }
                var regex = _checkNamesRegexCache.GetOrAdd(token, t =>
                {
                    var pattern = "^" + Regex.Escape(t).Replace(@"\*", ".*") + "$";
                    return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                });
                if (regex.IsMatch(name)) { return true; }
            }
        }

        return false;
    }

    private async Task AddGuestTasksTableAsync(ISectionWriter sw, ProgressTracker pt, string node, long vmId)
    {
        if (!settings.Guest.Detail.Tasks.Enabled) { return; }

        pt.Step("Tasks");
        var rows = (await client.Nodes[node].Tasks.GetAsync(errors: TrueOrNull(settings.Guest.Detail.Tasks.OnlyErrors),
                                                             limit: IntOrNull(settings.Guest.Detail.Tasks.MaxCount),
                                                             vmid: (int)vmId)
                                .ToSafeEnum(_issues, "Guest Tasks", LinkKey.Vm(vmId)))
                       .Select(a => new
                       {
                           a.UniqueTaskId,
                           a.Type,
                           a.User,
                           a.Status,
                           StatusOkFlag = ToX(a.StatusOk),
                           StartTime = a.StartTimeDate,
                           EndTime = a.EndTimeDate,
                           a.Duration
                       }).ToList();
        sw.AddTable("Tasks", rows);
    }

    private void BuildAndStoreNetworkDiagramSvg()
    {
        NetworkDiagramSvg = NetworkDiagramBuilder.BuildSvg(
            _pendingNodeNetworkRows.Select(r => new NetworkDiagramBuilder.NodeNetworkRow(r.Node, r.Network)),
            _sdnRows,
            _pendingNetworkRows.Select(r => new NetworkDiagramBuilder.VmNetworkRow(r.VmId, r.Name, r.Node, r.Type, r.Status, r.Hostname, r.Network, r.IsInternal)),
            _storageConfigs,
            new NetworkDiagramBuilder.DiagramInfo(info.ApplicationName, info.ApplicationUrl, info.ApplicationVersion));

        _pendingNodeNetworkRows.Clear();
        _pendingNetworkRows.Clear();
    }

    private static void AddLogs(ISectionWriter sw, string title, IEnumerable<string>? logs)
    {
        var list = (logs ?? []).ToList();

        // Proxmox API returns "no content" string when there are no logs,
        // so we need to check for that and return an empty list instead
        if (list.Count > 0 && list[0] == "no content") { list = []; }

        sw.AddTable(title, list.ConvertAll(a => new { Log = a }));
    }
}
