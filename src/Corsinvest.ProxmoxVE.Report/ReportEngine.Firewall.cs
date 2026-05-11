/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddFirewallDataAsync()
    {
        if (!settings.Firewall.Enabled) { return 0; }

        using var sw = _writer.AddSection("Firewall");

        ITableHandle? rulesTable = null;
        ITableHandle? aliasTable = null;
        ITableHandle? ipSetTable = null;
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
            if (rulesTable == null) { rulesTable = sw.AddTable("Firewall Rules", rows); }
            else { sw.AppendData(rulesTable, rows); }
        }

        void AppendAliases(string scopeType, string scope, string scopeName, IEnumerable<FirewallAlias> aliases)
        {
            var rows = aliases.Select(a => new
            {
                ScopeType = scopeType,
                Scope = scope,
                ScopeName = scopeName,
                a.Name,
                a.Cidr,
                a.IpVersion,
                CommentWrap = a.Comment,
            }).ToList();

            if (aliasTable == null) { aliasTable = sw.AddTable("Firewall Aliases", rows); }
            else { sw.AppendData(aliasTable, rows); }
        }

        void AppendIpSets(string scopeType, string scope, string scopeName, IEnumerable<FirewallIpSet> ipSets)
        {
            var rows = ipSets.Select(a => new
            {
                ScopeType = scopeType,
                Scope = scope,
                ScopeName = scopeName,
                a.Name,
                CommentWrap = a.Comment,
            }).ToList();

            if (ipSetTable == null) { ipSetTable = sw.AddTable("Firewall IPSets", rows); }
            else { sw.AppendData(ipSetTable, rows); }
        }

        ReportGlobal("Firewall: Cluster");
        var clusterRulesTask = client.Cluster.Firewall.Rules.GetAsync().ToSafeEnum(_issues, "Firewall", LinkKey.Firewall);
        var clusterAliasesTask = client.Cluster.Firewall.Aliases.GetAsync().ToSafeEnum(_issues, "Firewall", LinkKey.Firewall);
        var clusterIpSetsTask = client.Cluster.Firewall.Ipset.GetAsync().ToSafeEnum(_issues, "Firewall", LinkKey.Firewall);
        await Task.WhenAll(clusterRulesTask, clusterAliasesTask, clusterIpSetsTask);

        AppendRules("cluster", "cluster", "", clusterRulesTask.Result);
        AppendAliases("cluster", "cluster", "", clusterAliasesTask.Result);
        AppendIpSets("cluster", "cluster", "", clusterIpSetsTask.Result);

        var nodeFirewallResults = await RunParallelAsync(
            GetResources(ClusterResourceType.Node).Where(a => !a.IsUnknown),
            async item =>
            {
                ReportGlobal($"Firewall: Node {item.Node}");
                var rules = await client.Nodes[item.Node].Firewall.Rules.GetAsync()
                                        .ToSafeEnum(_issues, "Firewall", LinkKey.Node(item.Node));
                return (scope: item.Node, rules);
            });

        foreach (var (scope, rules) in nodeFirewallResults.OrderBy(r => r.scope))
        {
            AppendRules("node", scope, "", rules);
        }

        var guestFirewallResults = await RunParallelAsync(
            GetResources(ClusterResourceType.Vm).Where(a => !a.IsUnknown),
            async item =>
            {
                ReportGlobal($"Firewall: {item.VmType} {item.VmId}");

                var vmFw = client.Nodes[item.Node].Qemu[item.VmId].Firewall;
                var ctFw = client.Nodes[item.Node].Lxc[item.VmId].Firewall;

                var (rulesRaw, aliasesRaw, ipSetsRaw) = item.VmType switch
                {
                    VmType.Qemu => (vmFw.Rules.GetAsync(), vmFw.Aliases.GetAsync(), vmFw.Ipset.GetAsync()),
                    VmType.Lxc => (ctFw.Rules.GetAsync(), ctFw.Aliases.GetAsync(), ctFw.Ipset.GetAsync()),
                    _ => throw new InvalidOperationException($"unexpected VM type {item.VmType}"),
                };

                var vmLink = LinkKey.Vm(item.VmId);
                var rulesTask = rulesRaw.ToSafeEnum(_issues, "Firewall", vmLink);
                var aliasesTask = aliasesRaw.ToSafeEnum(_issues, "Firewall", vmLink);
                var ipSetsTask = ipSetsRaw.ToSafeEnum(_issues, "Firewall", vmLink);
                await Task.WhenAll(rulesTask, aliasesTask, ipSetsTask);

                return (scopeType: item.Type,
                        scope: item.VmId.ToString(),
                        scopeName: item.Name,
                        rules: rulesTask.Result,
                        aliases: aliasesTask.Result,
                        ipSets: ipSetsTask.Result);
            });

        foreach (var (scopeType, scope, scopeName, rules, aliases, ipSets) in guestFirewallResults.OrderBy(r => r.scope))
        {
            AppendRules(scopeType, scope, scopeName, rules);
            AppendAliases(scopeType, scope, scopeName, aliases);
            AppendIpSets(scopeType, scope, scopeName, ipSets);
        }

        return rulesCount;
    }
}
