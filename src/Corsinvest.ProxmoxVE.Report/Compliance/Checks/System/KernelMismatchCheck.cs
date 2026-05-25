/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.System;

/// <summary>
/// Flags nodes running a kernel that is older than the majority of the cluster.
/// Indicates a pending reboot after a kernel update, or a node skipped during the
/// rolling upgrade — both are configuration-management findings.
/// Mapped by ISO 27001:2022 A.8.8 / A.8.9 and NIS2 Art. 21(e).
/// </summary>
internal sealed class KernelMismatchCheck : IComplianceCheck
{
    public string Id => "system.node-kernel-mismatch";
    public string Title => "All cluster nodes should run the same kernel release";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Nodes];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var nodes = ctx.Get<NodeInfo>(ComplianceDataKind.Nodes)
                       .Where(n => n.IsOnline && !string.IsNullOrEmpty(n.KernelRelease))
                       .ToList();

        if (nodes.Count <= 1) { yield break; }

        var distinct = nodes.Select(n => n.KernelRelease).Distinct(StringComparer.Ordinal).ToList();
        if (distinct.Count == 1) { yield break; }

        var majority = nodes.GroupBy(n => n.KernelRelease!)
                            .OrderByDescending(g => g.Count())
                            .First().Key;

        foreach (var n in nodes)
        {
            if (string.Equals(n.KernelRelease, majority, StringComparison.Ordinal)) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Low,
                ScopeType = "node",
                Scope = n.Node,
                Title = "Kernel mismatch with cluster majority",
                Details = $"Node {n.Node} runs kernel {n.KernelRelease}; majority of the cluster runs {majority}.",
                Remediation = "Reboot the node to load the updated kernel, or apply the missing update.",
            };
        }
    }
}
