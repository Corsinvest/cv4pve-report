/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using System.Text.RegularExpressions;

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>Report engine for Proxmox VE infrastructure.</summary>
public partial class ReportEngine(PveClient client, Settings settings, ReportInfo info)
{
    /// <summary>Optional provider to calculate snapshot size. Parameters: node, vmType, vmId, snapName. Returns size in bytes or null if unavailable.</summary>
    public Func<string, VmType, long, string, Task<long?>>? SnapshotSizeProvider { get; set; }

    private IProgress<ReportProgress>? _progress;
    private readonly Dictionary<string, string> _sheetLinks = [];
    private SheetWriter? _networkSw;
    private IXLTable? _networkNodeTable;
    private IXLTable? _networkVmTable;
    private SheetWriter? _diskSw;
    private IXLTable? _diskTable;
    private SheetWriter? _partitionSw;
    private IXLTable? _partitionTable;
    private List<ClusterResource> _resources = [];
    private HashSet<long> _vmIds = [];
    private readonly Dictionary<string, int> _usedSheetNames = [];

    private const int MaxSheetNameLength = 31;

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

    private record VmNetworkRow(long VmId,
                            string Name,
                            string Node,
                            string Type,
                            string Status,
                            string Hostname,
                            VmNetwork Network,
                            bool IsInternal);

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

    private async Task BuildSheetLinksAsync()
    {
        _resources = [.. (await client.GetResourcesAsync(ClusterResourceType.All))];
        _resources.CalculateHostUsage();
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
        var stream = new MemoryStream();
        await GenerateExcelAsync(stream);
        stream.Position = 0;
        return stream;
    }

    private void ReportGlobal(string step)
        => _progress?.Report(new ReportProgress { Step = step });

    private async Task GenerateExcelAsync(Stream stream)
    {
        ReportGlobal("Init");
        await BuildSheetLinksAsync();

        using var workbook = new XLWorkbook();
        ConfigureWorkbook(workbook);

        ReportGlobal("Cluster");
        await AddClusterDataAsync(workbook);

        // Register storage sheet links before VM/CT/Node processing so disk links resolve correctly
        ReportGlobal("Storages");
        await AddStoragesDataAsync(workbook);

        ReportGlobal("Nodes");
        await AddNodesDataAsync(workbook);

        ReportGlobal("Vms");
        await AddVmsDataAsync(workbook);

        ReportGlobal("Containers");
        await AddContainersDataAsync(workbook);

        ReportGlobal("Network");
        _networkSw?.WriteIndex();
        _networkSw?.AdjustColumns();

        ReportGlobal("Storage Content");
        await AddStorageContentDataAsync(workbook);

        ReportGlobal("Disks");
        _diskSw?.WriteIndex();
        _diskSw?.AdjustColumns();

        ReportGlobal("Partitions");
        _partitionSw?.AdjustColumns();

        ReportGlobal("Snapshots");
        await AddSnapshotsDataAsync(workbook);

        ReportGlobal("Firewall");
        await AddFirewallDataAsync(workbook);

        ReportGlobal("Replication");
        await AddReplicationDataAsync(workbook);

        ReportGlobal("RRD Nodes");
        await AddRrdNodeDataAsync(workbook);

        ReportGlobal("RRD Storage");
        await AddRrdStorageDataAsync(workbook);

        ReportGlobal("RRD Guests");
        await AddRrdGuestDataAsync(workbook);

        ReportGlobal("Syslog");
        await AddSyslogDataAsync(workbook);

        ReportGlobal("Cluster Log");
        await AddClusterLogDataAsync(workbook);

        ReportGlobal("Cluster Tasks");
        await AddClusterTasksDataAsync(workbook);

        ReportGlobal("Cover");
        AddCoverPage(workbook);

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
            foreach (var ws in sheets)
            {
                ws.Position = pos++;
            }
        }

        void PlaceByPrefix(string prefix) =>
            Place(workbook.Worksheets.Where(w => w.Name == prefix || w.Name.StartsWith(prefix + " ") || w.Name.StartsWith(prefix + "_")));

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
    private static string ToX(bool value) => value ? "X" : "";
    private static string ToX(bool? value) => value == true ? "X" : "";
    private static string ToNewLine(string? value, string character = ",")
        => value?.Replace(character, Environment.NewLine) ?? "";

    private static DateTime? FromUnixTime(long seconds)
        => seconds == 0
            ? null
            : DateTimeOffset.FromUnixTimeSeconds(seconds).DateTime;

    private static readonly Dictionary<string, Regex> _checkNamesRegexCache = [];

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
                if (!_checkNamesRegexCache.TryGetValue(token, out var regex))
                {
                    var pattern = "^" + Regex.Escape(token).Replace(@"\*", ".*") + "$";
                    regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    _checkNamesRegexCache[token] = regex;
                }
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
        if (!settings.Guest.Tasks.Enabled) { return; }
        pt.Step("Tasks");
        var taskSettings = settings.Guest.Tasks;
        sw.CreateTable("Tasks",
                       (await client.Nodes[node].Tasks.GetAsync(vmid: (int)vmId,
                                                                errors: taskSettings.OnlyErrors
                                                                            ? true
                                                                            : null,
                                                                limit: taskSettings.MaxCount > 0
                                                                            ? taskSettings.MaxCount
                                                                            : null
                       )).Select(a => new
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

    private static void AddLogs(SheetWriter sw, string title, IEnumerable<string> logs)
    {
        var list = logs.ToList();

        // Proxmox API returns "no content" string when there are no logs, 
        // so we need to check for that and return an empty list instead
        if (list.Count > 0 && list[0] == "no content") { list = []; }

        sw.CreateTable(title, list.Select(a => new { Log = a }));
    }
}
