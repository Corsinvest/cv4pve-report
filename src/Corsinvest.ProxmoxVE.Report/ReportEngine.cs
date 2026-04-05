/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using System.Text.RegularExpressions;

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>Report engine for Proxmox VE infrastructure.</summary>
public partial class ReportEngine(PveClient client, Settings settings, ReportInfo info)
{
    private IProgress<ReportProgress>? _progress;
    private readonly Dictionary<string, string> _sheetLinks = [];
    private readonly Dictionary<string, IEnumerable<NodeNetwork>> _nodeNetworks = [];
    private readonly List<VmNetworkRow> _vmNetworkRows = [];
    private readonly List<VmDiskRow> _vmDiskRows = [];

    private string? GetSheetName(ClusterResourceType type, params string[] values)
        => _sheetLinks.TryGetValue(SheetLinkKey(type, values), out var name)
            ? name
            : null;

    private SheetWriter CreateSheetWriter(XLWorkbook workbook, string name)
        => new(workbook.Worksheets.Add(name), _sheetLinks)
        {
            SkipEmptyCollections = settings.SkipEmptyCollections
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
        foreach (var item in await client.Cluster.Resources.GetAsync())
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

        ReportGlobal("Cover");
        AddCoverPage(workbook);

        ReportGlobal("Cluster");
        await AddClusterDataAsync(workbook);

        ReportGlobal("Storages");
        await AddStoragesDataAsync(workbook);

        ReportGlobal("Nodes");
        await AddNodesDataAsync(workbook);

        ReportGlobal("Vms");
        await AddVmsDataAsync(workbook);

        ReportGlobal("Network");
        await AddNetworkDataAsync(workbook);

        ReportGlobal("Disks");
        await AddDisksDataAsync(workbook);

        ReportGlobal("Saving");
        workbook.SaveAs(stream);
    }

    private static double ToGB(double bytes) => Math.Round(bytes / 1024 / 1024 / 1024, 2);
    private static double ToMB(double bytes) => Math.Round(bytes / 1024 / 1024, 2);

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

    private static void ConfigureWorkbook(XLWorkbook workbook)
    {
        workbook.Author = "cv4pve-report";
        workbook.Properties.Author = "cv4pve-report Corsinvest Srl";
        workbook.Properties.Title = "Infrastructure Report";
        workbook.Properties.Subject = "cv4pve-report System Report";
        workbook.Properties.Category = "IT Infrastructure";
        workbook.Properties.Comments = "Automated report generated by cv4pve-report for Proxmox VE";
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
                              a.Source,
                              a.Dest,
                              a.Protocol,
                              a.DestinationPort,
                              a.SourcePort,
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
