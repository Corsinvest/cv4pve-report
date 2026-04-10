/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private SheetWriter? _firewallSw;
    private IXLTable? _firewallRulesTable;
    private IXLTable? _firewallAliasTable;
    private IXLTable? _firewallIpSetTable;

    private async Task<int> AddFirewallDataAsync(XLWorkbook workbook)
    {
        if (!settings.Firewall.Enabled) { return 0; }

        var semaphore = CreateSemaphore();

        // Cluster firewall
        ReportGlobal("Firewall: Cluster");
        var clusterRules = await client.Cluster.Firewall.Rules.GetAsync();
        var clusterAliases = await client.Cluster.Firewall.Aliases.GetAsync();
        var clusterIpSets = await client.Cluster.Firewall.Ipset.GetAsync();

        AppendFirewallRules(workbook, "cluster", "cluster", "", clusterRules);
        AppendFirewallAlias(workbook, "cluster", "cluster", "", clusterAliases);
        AppendFirewallIpSet(workbook, "cluster", "cluster", "", clusterIpSets);

        // Nodes firewall — parallel
        var nodes = GetResources(ClusterResourceType.Node)
                              .Where(a => !a.IsUnknown)
                              .ToList();

        var nodeTasks = nodes.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                ReportGlobal($"Firewall: Node {item.Node}");
                return (scopeType: "node",
                        scope: item.Node,
                        scopeName: "",
                        rules: await client.Nodes[item.Node].Firewall.Rules.GetAsync());
            }
            finally { semaphore.Release(); }
        });

        foreach (var (scopeType, scope, scopeName, rules) in (await Task.WhenAll(nodeTasks)).OrderBy(r => r.scope))
        {
            AppendFirewallRules(workbook, scopeType, scope, scopeName, rules);
        }

        // VMs and CTs firewall — parallel
        var guests = GetResources(ClusterResourceType.Vm)
                               .Where(a => !a.IsUnknown)
                               .ToList();

        var guestTasks = guests.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                ReportGlobal($"Firewall: {item.VmType} {item.VmId}");
                var scopeType = item.VmType == VmType.Qemu ? "qemu" : "lxc";
                var scope = item.VmId.ToString();

                IEnumerable<FirewallRule> rules;
                IEnumerable<FirewallAlias> aliases;
                IEnumerable<FirewallIpSet> ipSets;

                if (item.VmType == VmType.Qemu)
                {
                    var vmFw = client.Nodes[item.Node].Qemu[item.VmId].Firewall;
                    rules = await vmFw.Rules.GetAsync();
                    aliases = await vmFw.Aliases.GetAsync();
                    ipSets = await vmFw.Ipset.GetAsync();
                }
                else
                {
                    var ctFw = client.Nodes[item.Node].Lxc[item.VmId].Firewall;
                    rules = await ctFw.Rules.GetAsync();
                    aliases = await ctFw.Aliases.GetAsync();
                    ipSets = await ctFw.Ipset.GetAsync();
                }

                return (scopeType, scope, scopeName: item.Name, rules, aliases, ipSets);
            }
            finally { semaphore.Release(); }
        });

        foreach (var (scopeType, scope, scopeName, rules, aliases, ipSets) in (await Task.WhenAll(guestTasks)).OrderBy(r => r.scope))
        {
            AppendFirewallRules(workbook, scopeType, scope, scopeName, rules);
            AppendFirewallAlias(workbook, scopeType, scope, scopeName, aliases);
            AppendFirewallIpSet(workbook, scopeType, scope, scopeName, ipSets);
        }

        _firewallSw?.WriteIndex();
        _firewallSw?.AdjustColumns();

        return clusterRules.Count() + nodes.Count + guests.Count;
    }

    private void AppendFirewallRules(XLWorkbook workbook,
                                     string scopeType,
                                     string scope,
                                     string scopeName,
                                     IEnumerable<FirewallRule> rules)
    {
        var rows = rules.Select(a => new
        {
            ScopeType = scopeType,
            Scope = scope,
            ScopeName = scopeName,
            a.Positon,
            a.Type,
            a.Action,
            EnableFlag = ToX(a.Enable),
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
            CommentWrap = a.Comment,
        }).ToList();

        EnsureFirewallSheet(workbook);
        if (_firewallRulesTable == null)
        {
            _firewallSw!.ReserveIndexRows(3);
            _firewallRulesTable = _firewallSw.CreateTable("Firewall Rules", rows);
        }
        else
        {
            _firewallSw!.AppendData(_firewallRulesTable, rows);
        }
    }

    private void AppendFirewallAlias(XLWorkbook workbook,
                                     string scopeType,
                                     string scope,
                                     string scopeName,
                                     IEnumerable<FirewallAlias> aliases)
    {
        EnsureFirewallSheet(workbook);
        _firewallSw!.CreateOrAddTable(ref
                                      _firewallAliasTable,
                                      "Firewall Aliases",
                                      aliases.Select(a => new
                                      {
                                          ScopeType = scopeType,
                                          Scope = scope,
                                          ScopeName = scopeName,
                                          a.Name,
                                          a.Cidr,
                                          a.IpVersion,
                                          CommentWrap = a.Comment,
                                      }).ToList());
    }

    private void AppendFirewallIpSet(XLWorkbook workbook,
                                     string scopeType,
                                     string scope,
                                     string scopeName,
                                     IEnumerable<FirewallIpSet> ipSets)
    {

        EnsureFirewallSheet(workbook);
        _firewallSw!.CreateOrAddTable(ref
                                      _firewallIpSetTable,
                                      "Firewall IPSets",
                                      ipSets.Select(a => new
                                      {
                                          ScopeType = scopeType,
                                          Scope = scope,
                                          ScopeName = scopeName,
                                          a.Name,
                                          CommentWrap = a.Comment,
                                      }).ToList());
    }

    private void EnsureFirewallSheet(XLWorkbook workbook)
        => _firewallSw ??= CreateSheetWriter(workbook, "Firewall");
}
