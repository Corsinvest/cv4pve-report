/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.System;

/// <summary>
/// Flags clusters where nodes run different Proxmox VE versions. A heterogeneous
/// cluster is acceptable during a rolling upgrade but should not be the steady
/// state — version drift causes feature parity issues and unpredictable behaviour.
/// Mapped by ISO 27001:2022 A.8.9 (configuration management) and NIS2 Art. 21(e).
/// </summary>
internal sealed class PveVersionMismatchCheck : IComplianceCheck
{
    public string Id => "system.node-pve-version-mismatch";
    public string Title => "All cluster nodes should run the same Proxmox VE version";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Nodes];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var nodes = ctx.Get<NodeInfo>(ComplianceDataKind.Nodes)
                       .Where(n => n.IsOnline && !string.IsNullOrEmpty(n.PveVersion))
                       .ToList();

        if (nodes.Count <= 1) { yield break; }

        var distinct = nodes.Select(n => n.PveVersion).Distinct(StringComparer.Ordinal).ToList();
        if (distinct.Count == 1) { yield break; }

        var majority = nodes.GroupBy(n => n.PveVersion!)
                            .OrderByDescending(g => g.Count())
                            .First().Key;

        foreach (var n in nodes)
        {
            if (string.Equals(n.PveVersion, majority, StringComparison.Ordinal)) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Low,
                ScopeType = "node",
                Scope = n.Node,
                Title = "PVE version drift",
                Details = $"Node {n.Node} runs PVE {n.PveVersion}; majority of the cluster runs {majority}.",
                Remediation = "Schedule a rolling upgrade so all nodes converge on the same Proxmox VE version.",
            };
        }
    }
}
