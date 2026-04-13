/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>Report engine for Proxmox VE infrastructure.</summary>
public partial class ReportEngine(PveClient client, Settings settings, ReportInfo info)
{
    /// <summary>Optional provider to calculate snapshot size. Parameters: node, vmType, vmId, snapName. Returns size in bytes or null if unavailable.</summary>
    public Func<string, VmType, long, string, Task<long>>? SnapshotSizeProvider { get; set; }

    private IProgress<ReportProgress>? _progress;
    private readonly Dictionary<string, string> _sheetLinks = [];
    private List<ClusterResource> _resources = [];
    private Dictionary<long, ClusterResource> _resourcesByVmId = [];
    private List<ClusterResource> _uniqueStorages = [];
    private readonly List<VmNetworkRow> _pendingNetworkRows = [];
    private readonly List<(string Node, NodeNetwork Network)> _pendingNodeNetworkRows = [];
    private readonly List<(ClusterResource Vm, IEnumerable<VmDisk> Disks)> _pendingDiskRows = [];
    private readonly List<(ClusterResource Vm, IEnumerable<VmQemuAgentGetFsInfo.ResultInfo> Partitions)> _pendingPartitionRows = [];
    private HashSet<long> _vmIds = [];
    private readonly Dictionary<string, int> _usedSheetNames = [];

    private const int MaxSheetNameLength = 31;

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

    private string SafeSheetName(string candidate)
    {
        var name = candidate.Length <= MaxSheetNameLength ? candidate : candidate[..MaxSheetNameLength];
        if (!_usedSheetNames.TryGetValue(name, out var count))
        {
            _usedSheetNames[name] = 1;
            return name;
        }
        count++;
        _usedSheetNames[name] = count;
        var suffix = $"_{count}";
        return name[..Math.Min(name.Length, MaxSheetNameLength - suffix.Length)] + suffix;
    }

    private string? GetSheetName(ClusterResourceType type, params string[] values)
        => _sheetLinks.TryGetValue(SheetLinkKey(type, values), out var name)
            ? name
            : null;

    private SheetWriter CreateSheetWriter(XLWorkbook workbook, string name)
    {
        var safeName = SafeSheetName(name);
        // Update any sheetLink that points to this logical name to the actual safe name
        foreach (var key in _sheetLinks.Where(kv => kv.Value == name).Select(kv => kv.Key).ToList())
        {
            _sheetLinks[key] = safeName;
        }

        return new(workbook.Worksheets.Add(safeName), _sheetLinks);
    }

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

        _sheetLinks["storage:link"] = "Storages";
        _sheetLinks["list:nodes"] = "Nodes";
        _sheetLinks["list:vms"] = "Vms";
        _sheetLinks["list:containers"] = "Containers";

        foreach (var item in _resources)
        {
            switch (item.ResourceType)
            {
                case ClusterResourceType.Node:
                    _sheetLinks[SheetLinkKey(ClusterResourceType.Node, item.Node)] = $"Node {item.Node}";
                    break;

                case ClusterResourceType.Vm:
                    _sheetLinks[SheetLinkKey(ClusterResourceType.Vm, item.VmId.ToString())] = $"{(item.VmType == VmType.Qemu ? "VM" : "CT")} {item.VmId}";
                    break;
            }
        }
    }

    /// <summary>Generates the Excel report and returns it as a stream.</summary>
    public async Task<Stream> GenerateAsync(IProgress<ReportProgress>? progress = null)
    {
        _progress = progress;
        var originalTimeout = client.Timeout;
        if (settings.ApiTimeout > 0) { client.Timeout = TimeSpan.FromSeconds(settings.ApiTimeout); }
        var stream = new MemoryStream();
        try
        {
            await GenerateExcelAsync(stream);
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

    private record SectionStat(string Name, int Count, TimeSpan Duration);

    private async Task GenerateExcelAsync(Stream stream)
    {
        ReportGlobal("Init");
        await LoadResourcesAsync();

        using var workbook = new XLWorkbook();
        ConfigureWorkbook(workbook);

        var sections = new Dictionary<string, Func<Task<int>>>
        {
            ["Cluster"] = () => AddClusterDataAsync(workbook),
            ["Storages"] = () => AddStoragesDataAsync(workbook),
            ["Nodes"] = () => AddNodesDataAsync(workbook),
            ["Vms"] = () => AddVmsDataAsync(workbook),
            ["Containers"] = () => AddContainersDataAsync(workbook),
            ["Network"] = () => Task.FromResult(WriteNetworkData(workbook)),
            ["Storage Content"] = () => AddStorageContentDataAsync(workbook),
            ["Disks"] = () => Task.FromResult(WriteDiskData(workbook)),
            ["Partitions"] = () => Task.FromResult(WritePartitionData(workbook)),
            ["Snapshots"] = () => AddSnapshotsDataAsync(workbook),
            ["Firewall"] = () => AddFirewallDataAsync(workbook),
            ["Replication"] = () => AddReplicationDataAsync(workbook),
            ["RRD Nodes"] = () => AddRrdNodeDataAsync(workbook),
            ["RRD Storage"] = () => AddRrdStorageDataAsync(workbook),
            ["RRD Guests"] = () => AddRrdGuestDataAsync(workbook),
            ["Syslog"] = () => AddSyslogDataAsync(workbook),
            ["Cluster Log"] = () => AddClusterLogDataAsync(workbook),
            ["Cluster Tasks"] = () => AddClusterTasksDataAsync(workbook),
        };

        var stats = new List<SectionStat>();
        var sw = Stopwatch.StartNew();
        foreach (var (name, action) in sections)
        {
            ReportGlobal(name);
            sw.Restart();
            stats.Add(new(name, await action(), sw.Elapsed));
        }

        ReportGlobal("Cover");
        AddCoverPage(workbook, stats);

        ReportGlobal("Reorder sheets");
        ReorderSheets(workbook);

        ReportGlobal("Saving");
        workbook.SaveAs(stream);
    }

    private static void ReorderSheets(XLWorkbook workbook)
    {
        var pos = 1;

        void Place(IEnumerable<IXLWorksheet> sheets)
        {
            foreach (var ws in sheets) { ws.Position = pos++; }
        }

        void PlaceByPrefix(string prefix) =>
            Place(workbook.Worksheets.Where(a => a.Name == prefix
                                                    || a.Name.StartsWith(prefix + " ")
                                                    || a.Name.StartsWith(prefix + "_")));

        void PlaceVmPrefix(string prefix) =>
            Place(workbook.Worksheets
                          .Where(w => w.Name.StartsWith(prefix + " "))
                          .OrderBy(w => int.TryParse(w.Name[(prefix.Length + 1)..], out var n) ? n : int.MaxValue));

        // Summary first
        PlaceByPrefix("Summary");

        // Cluster
        PlaceByPrefix("Cluster");

        // Nodes / VMs / Containers lists
        PlaceByPrefix("Nodes");
        PlaceByPrefix("Vms");
        PlaceByPrefix("Containers");

        // Disks / Partitions / Snapshots
        PlaceByPrefix("Disks");
        PlaceByPrefix("Partitions");
        PlaceByPrefix("Snapshots");

        // Network / Storage
        PlaceByPrefix("Network");
        PlaceByPrefix("Storages");
        PlaceByPrefix("Storage Content");
        PlaceByPrefix("Backups");

        // Firewall
        PlaceByPrefix("Firewall");

        // Replication
        PlaceByPrefix("Replication");

        // RRD / Historical
        PlaceByPrefix("RRD Nodes");
        PlaceByPrefix("RRD Storage");
        PlaceByPrefix("RRD Guests");
        PlaceByPrefix("Syslog");

        // Detail sheets at the end
        PlaceByPrefix("Node");
        PlaceVmPrefix("VM");
        PlaceVmPrefix("CT");
    }

    private static string StorageNode(ClusterResource item)
        => item.Shared
            ? "(shared)"
            : item.Node;

    private static double ToGB(double bytes) => Math.Round(bytes / 1024 / 1024 / 1024, 2);
    private static double ToMB(double bytes) => Math.Round(bytes / 1024 / 1024, 2);

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

    private void ConfigureWorkbook(XLWorkbook workbook)
    {
        workbook.Author = info.ApplicationName;
        workbook.Properties.Author = info.ApplicationName;
        workbook.Properties.Title = "Infrastructure Report";
        workbook.Properties.Subject = $"{info.ApplicationName} v{info.ApplicationVersion} System Report";
        workbook.Properties.Category = "IT Infrastructure";
        workbook.Properties.Comments = $"Automated report generated by {info.ApplicationName} for Proxmox VE";
        workbook.Properties.Company = "Corsinvest Srl";
    }

    private async Task AddGuestTasksTableAsync(SheetWriter sw, ProgressTracker pt, string node, long vmId)
    {
        if (!settings.Guest.Detail.Tasks.Enabled) { return; }

        pt.Step("Tasks");
        sw.CreateTable("Tasks",
                       (await client.Nodes[node].Tasks.GetAsync(errors: TrueOrNull(settings.Guest.Detail.Tasks.OnlyErrors),
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
                       }));
    }

    private static void AddLogs(SheetWriter sw, string title, IEnumerable<string>? logs)
    {
        var list = (logs ?? []).ToList();

        // Proxmox API returns "no content" string when there are no logs, 
        // so we need to check for that and return an empty list instead
        if (list.Count > 0 && list[0] == "no content") { list = []; }

        sw.CreateTable(title, list.Select(a => new { Log = a }));
    }
}
