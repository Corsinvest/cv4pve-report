/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Storage;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Report.Writers;
using Corsinvest.ProxmoxVE.Report.Writers.Xlsx;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

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
    private readonly List<(ClusterResource Vm, IEnumerable<VmDisk> Disks)> _pendingDiskRows = [];
    private readonly List<(ClusterResource Vm, IEnumerable<VmQemuAgentGetFsInfo.ResultInfo> Partitions)> _pendingPartitionRows = [];
    private HashSet<long> _vmIds = [];
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

    private string? GetSheetName(ClusterResourceType type, params string[] values)
        => _writer.Links.TryGetValue(SheetLinkKey(type, values), out var name)
            ? name
            : null;


    internal static string SheetLinkKey(ClusterResourceType type, params string[] values)
        => $"{type.ToString().ToLowerInvariant()}:{values.JoinAsString(":")}";

    private async Task LoadResourcesAsync()
    {
        _resources = [.. (await client.GetResourcesAsync(ClusterResourceType.All))];
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

        _storageConfigs = await client.Storage.GetAsync();

        _writer.Links["storage:link"] = "Storages";
        _writer.Links["list:nodes"] = "Nodes";
        _writer.Links["list:vms"] = "VMs";
        _writer.Links["list:containers"] = "Containers";

        foreach (var item in _resources)
        {
            switch (item.ResourceType)
            {
                case ClusterResourceType.Node:
                    _writer.Links[SheetLinkKey(ClusterResourceType.Node, item.Node)] = $"Node {item.Node}";
                    break;

                case ClusterResourceType.Vm:
                    _writer.Links[SheetLinkKey(ClusterResourceType.Vm, item.VmId.ToString())] = $"{VmTypeLabel(item.VmType)} {item.VmId}";
                    break;
            }
        }
    }

    /// <summary>Generates the report in the requested format and returns it as a stream.</summary>
    public async Task<Stream> GenerateAsync(ReportFormat format,
                                            IProgress<ReportProgress>? progress = null)
    {
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
            ReportFormat.Xlsx => new XlsxReportWriter(),
            ReportFormat.Html => new Writers.Html.HtmlReportWriter(),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
        };
        _writer.SetMetadata(info);

        await LoadResourcesAsync();

        var sections = new Dictionary<string, Func<Task<int>>>
        {
            ["Cluster"] = () => AddClusterDataAsync(),
            ["Storages"] = () => AddStoragesDataAsync(),
            ["Nodes"] = () => AddNodesDataAsync(),
            ["VMs"] = () => AddVmsDataAsync(),
            ["Containers"] = () => AddContainersDataAsync(),
            ["Network"] = () => Task.FromResult(WriteNetworkData()),
            ["Storage Content"] = () => AddStorageContentDataAsync(),
            ["Disks"] = () => Task.FromResult(WriteDiskData()),
            ["Partitions"] = () => Task.FromResult(WritePartitionData()),
            ["Snapshots"] = () => AddSnapshotsDataAsync(),
            ["Firewall"] = () => AddFirewallDataAsync(),
            ["Replication"] = () => AddReplicationDataAsync(),
            ["RRD Nodes"] = () => AddRrdNodeDataAsync(),
            ["RRD Storage"] = () => AddRrdStorageDataAsync(),
            ["RRD Guests"] = () => AddRrdGuestDataAsync(),
            ["Syslog"] = () => AddSyslogDataAsync(),
            ["Cluster Log"] = () => AddClusterLogDataAsync(),
            ["Cluster Tasks"] = () => AddClusterTasksDataAsync(),
        };

        var stats = new List<SectionStat>();
        var sw = Stopwatch.StartNew();
        foreach (var (name, action) in sections)
        {
            ReportGlobal(name);
            sw.Restart();
            stats.Add(new(name, await action(), sw.Elapsed));
        }

        ReportGlobal("Network Diagram");
        BuildAndStoreNetworkDiagramSvg();
        if (NetworkDiagramSvg is { Length: > 0 } svg) { _writer.SetNetworkDiagram(svg); }

        ReportGlobal("Cover");
        _writer.WriteCoverPage(info, settings, stats);

        ReportGlobal("Saving");
        await _writer.SaveAsync(stream);

        _writer.Dispose();
    }

    private static string StorageNode(ClusterResource item)
        => item.Shared
            ? "(shared)"
            : item.Node;

    private static double ToGB(double bytes) => Math.Round(bytes / 1024 / 1024 / 1024, 2);
    private static double ToMB(double bytes) => Math.Round(bytes / 1024 / 1024, 2);

    internal static string? LabeledValue(string label, string? value)
        => string.IsNullOrWhiteSpace(value) ? null : $"{label}: {value}";

    internal static string VmTypeLabel(VmType type) => type == VmType.Qemu ? "VM" : "CT";
    internal static string VmTypeLabel(string? type)
        => string.Equals(type, "qemu", StringComparison.OrdinalIgnoreCase) ? "VM" : "CT";

    private async Task<TResult[]> RunParallelAsync<T, TResult>(IEnumerable<T> source, Func<T, Task<TResult>> func)
    {
        var semaphore = new SemaphoreSlim(settings.MaxParallelRequests);
        return await Task.WhenAll(source.Select(async item =>
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
                                                            vmid: (int)vmId))
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

    private static void AddLogs(ISectionWriter sw, string title, IEnumerable<string>? logs)
    {
        var list = (logs ?? []).ToList();

        // Proxmox API returns "no content" string when there are no logs,
        // so we need to check for that and return an empty list instead
        if (list.Count > 0 && list[0] == "no content") { list = []; }

        sw.AddTable(title, list.ConvertAll(a => new { Log = a }));
    }
}
