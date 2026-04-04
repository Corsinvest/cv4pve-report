/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Storage;

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>Report engine for Proxmox VE infrastructure.</summary>
public partial class ReportEngine(PveClient client, Settings settings, ReportInfo info)
{
    private IProgress<ReportProgress>? _progress;
    private readonly Dictionary<string, string> _sheetLinks = [];
    private readonly Dictionary<string, IEnumerable<NodeNetwork>> _nodeNetworks = [];
    private readonly List<VmNetworkRow> _vmNetworkRows = [];
    private readonly List<VmDiskRow> _vmDiskRows = [];
    private readonly List<dynamic> _storageRows = [];
    private IEnumerable<StorageItem>? _clusterStorageRows;
    private IEnumerable<ClusterResource> _resources = [];

    private void WriteStorage(SheetWriter sw)
    {
        sw.CreateTable("Storages", _storageRows, tbl =>
        {
            sw.ApplyNodeLinks(tbl);
            sw.ApplyStorageLinks(tbl);
        });
    }

    private async Task WriteClusterStorageAsync(SheetWriter sw, string title)
    {
        _clusterStorageRows ??= await client.Storage.GetAsync();

        sw.CreateTable(title,
                       _clusterStorageRows.Select(a => new
                       {
                           a.Storage,
                           a.Type,
                           a.Content,
                           a.Shared,
                           a.Disable,
                           a.Nodes,
                           a.Path,
                           a.Mountpoint,
                           a.Server,
                           a.Export,
                           a.Datastore,
                           a.Pool,
                           a.Username,
                           a.Monhost,
                           a.Sparse,
                           a.Krbd,
                           a.Preallocation,
                           a.PruneBackups,
                       }));
    }

    private string? GetSheetName(ClusterResourceType type, params string[] values)
        => _sheetLinks.TryGetValue(SheetLinkKey(type, values), out var name)
            ? name
            : null;

    private SheetWriter CreateSheetWriter(XLWorkbook workbook, string name)
        => new(workbook.Worksheets.Add(name), _sheetLinks)
        {
            SkipEmptyCollections = settings.SkipEmptyTables
        };

    internal static string SheetLinkKey(ClusterResourceType type, params string[] values)
        => $"{type.ToString().ToLowerInvariant()}:{values.JoinAsString(":")}";

    private const int MaxSheetNameLength = 31;

    private static string BuildStorageSheetName(string node, string storage)
    {
        var full = $"Storage {node} - {storage}";
        if (full.Length <= MaxSheetNameLength) { return full; }

        var noPrefix = $"{node} - {storage}";
        if (noPrefix.Length <= MaxSheetNameLength) { return noPrefix; }

        if (storage.Length <= MaxSheetNameLength) { return storage; }

        return storage[..MaxSheetNameLength];
    }

    private async Task BuildSheetLinksAsync()
    {
        _resources = await client.GetResourcesAsync(ClusterResourceType.All);
        _resources.CalculateHostUsage();

        foreach (var item in _resources)
        {
            switch (item.ResourceType)
            {
                case ClusterResourceType.Node:
                    _sheetLinks[SheetLinkKey(ClusterResourceType.Node, item.Node)] = $"Node {item.Node}";
                    break;

                case ClusterResourceType.Vm:
                    _sheetLinks[SheetLinkKey(ClusterResourceType.Vm, item.VmId.ToString())] = $"VM {item.VmId}";
                    break;

                case ClusterResourceType.Storage:
                    _sheetLinks[SheetLinkKey(ClusterResourceType.Storage, item.Node, item.Storage)] = BuildStorageSheetName(item.Node, item.Storage);
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

        // ReportGlobal("Cover");
        // AddCoverPage(workbook);

        // ReportGlobal("Cluster");
        // await AddClusterDataAsync(workbook);

        // ReportGlobal("Storages");
        // await AddStoragesDataAsync(workbook);

        // ReportGlobal("Nodes");
        // await AddNodesDataAsync(workbook);

        // ReportGlobal("Vms");
        // await AddVmsDataAsync(workbook);

        // ReportGlobal("Network");
        // await AddNetworkDataAsync(workbook);

        // ReportGlobal("Disks");
        // await AddDisksDataAsync(workbook);

        ReportGlobal("Syslog");
        await AddSyslogDataAsync(workbook);

        ReportGlobal("Saving");
        workbook.SaveAs(stream);
    }

    private static double ToGB(double bytes) => Math.Round(bytes / 1024 / 1024 / 1024, 2);
    private static double ToMB(double bytes) => Math.Round(bytes / 1024 / 1024, 2);
    private static string ToX(bool value) => value ? "X" : string.Empty;

    private static DateTime? FromUnixTime(long seconds)
        => seconds == 0
            ? null
            : DateTimeOffset.FromUnixTimeSeconds(seconds).DateTime;

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
                var pattern = "^" + Regex.Escape(token).Replace(@"\*", ".*") + "$";
                if (Regex.IsMatch(name, pattern, RegexOptions.IgnoreCase)) { return true; }
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

    private static void AddFirewallRules(SheetWriter sw, IEnumerable<FirewallRule> rules)
        => sw.CreateTable("Firewall Rules",
                          rules.Select(a => new
                          {
                              a.Positon,
                              a.Type,
                              a.Action,
                              a.Enable,
                              a.Macro,
                              a.Iface,
                              a.IpVersion,
                              a.Protocol,
                              a.IcmpType,
                              a.Source,
                              a.Dest,
                              a.DestinationPort,
                              a.SourcePort,
                              a.Log,
                              a.Comment
                          }));

    private static void AddFirewallAlias(SheetWriter sw, IEnumerable<FirewallAlias> alias)
        => sw.CreateTable("Firewall Alias",
                          alias.Select(a => new
                          {
                              a.Name,
                              a.Cidr,
                              a.IpVersion,
                              a.Comment
                          }));

    private static void AddFirewallIpSet(SheetWriter sw, IEnumerable<FirewallIpSet> ipSets)
        => sw.CreateTable("Firewall Alias",
                          ipSets.Select(a => new
                          {
                              a.Name,
                              a.Comment
                          }));
    private static void AddLogs(SheetWriter sw, string title, IEnumerable<string> logs)
        => sw.CreateTable(title, logs.Select(a => new { Log = a, }));

}
