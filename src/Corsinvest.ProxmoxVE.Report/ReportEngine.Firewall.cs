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
    private async Task<int> AddFirewallDataAsync(XLWorkbook workbook)
    {
        if (!settings.Firewall.Enabled) { return 0; }

        var sw = CreateSheetWriter(workbook, "Firewall");
        IXLTable? rulesTable = null;
        IXLTable? aliasTable = null;
        IXLTable? ipSetTable = null;
        var rulesCount = 0;

        void AppendRules(string scopeType, string scope, string scopeName, IEnumerable<FirewallRule> rules)
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

            rulesCount += rows.Count;
            if (rulesTable == null)
            {
                sw!.ReserveIndexRows(3);
                rulesTable = sw.CreateTable("Firewall Rules", rows);
            }
            else
            {
                sw!.AppendData(rulesTable, rows);
            }
        }

        void AppendAliases(string scopeType, string scope, string scopeName, IEnumerable<FirewallAlias> aliases)
            => sw.CreateOrAddTable(ref aliasTable,
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

        void AppendIpSets(string scopeType, string scope, string scopeName, IEnumerable<FirewallIpSet> ipSets)
            => sw.CreateOrAddTable(ref ipSetTable,
                                   "Firewall IPSets",
                                   ipSets.Select(a => new
                                   {
                                       ScopeType = scopeType,
                                       Scope = scope,
                                       ScopeName = scopeName,
                                       a.Name,
                                       CommentWrap = a.Comment,
                                   }).ToList());

        var semaphore = CreateSemaphore();

        // Cluster firewall
        ReportGlobal("Firewall: Cluster");
        var clusterRulesTask = client.Cluster.Firewall.Rules.GetAsync();
        var clusterAliasesTask = client.Cluster.Firewall.Aliases.GetAsync();
        var clusterIpSetsTask = client.Cluster.Firewall.Ipset.GetAsync();
        await Task.WhenAll(clusterRulesTask, clusterAliasesTask, clusterIpSetsTask);

        AppendRules("cluster", "cluster", "", clusterRulesTask.Result);
        AppendAliases("cluster", "cluster", "", clusterAliasesTask.Result);
        AppendIpSets("cluster", "cluster", "", clusterIpSetsTask.Result);

        // Nodes firewall — parallel
        var nodeTasks = GetResources(ClusterResourceType.Node)
                              .Where(a => !a.IsUnknown)
                              .Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                ReportGlobal($"Firewall: Node {item.Node}");
                return (scope: item.Node,
                        rules: await client.Nodes[item.Node].Firewall.Rules.GetAsync());
            }
            finally { semaphore.Release(); }
        });

        foreach (var (scope, rules) in (await Task.WhenAll(nodeTasks)).OrderBy(r => r.scope))
        {
            AppendRules("node", scope, "", rules);
        }

        // VMs and CTs firewall — parallel
        var guestTasks = GetResources(ClusterResourceType.Vm)
                               .Where(a => !a.IsUnknown)
                               .Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                ReportGlobal($"Firewall: {item.VmType} {item.VmId}");

                Task<IEnumerable<FirewallRule>> rulesTask;
                Task<IEnumerable<FirewallAlias>> aliasesTask;
                Task<IEnumerable<FirewallIpSet>> ipSetsTask;

                if (item.VmType == VmType.Qemu)
                {
                    var vmFw = client.Nodes[item.Node].Qemu[item.VmId].Firewall;
                    rulesTask = vmFw.Rules.GetAsync();
                    aliasesTask = vmFw.Aliases.GetAsync();
                    ipSetsTask = vmFw.Ipset.GetAsync();
                }
                else
                {
                    var ctFw = client.Nodes[item.Node].Lxc[item.VmId].Firewall;
                    rulesTask = ctFw.Rules.GetAsync();
                    aliasesTask = ctFw.Aliases.GetAsync();
                    ipSetsTask = ctFw.Ipset.GetAsync();
                }

                await Task.WhenAll(rulesTask, aliasesTask, ipSetsTask);

                return (scopeType: item.Type,
                        scope: item.VmId.ToString(),
                        scopeName: item.Name,
                        rules: rulesTask.Result,
                        aliases: aliasesTask.Result,
                        ipSets: ipSetsTask.Result);
            }
            finally { semaphore.Release(); }
        });

        foreach (var (scopeType, scope, scopeName, rules, aliases, ipSets) in (await Task.WhenAll(guestTasks)).OrderBy(r => r.scope))
        {
            AppendRules(scopeType, scope, scopeName, rules);
            AppendAliases(scopeType, scope, scopeName, aliases);
            AppendIpSets(scopeType, scope, scopeName, ipSets);
        }

        sw?.WriteIndex();
        sw?.AdjustColumns();

        return rulesCount;
    }
}
