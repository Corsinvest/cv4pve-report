/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.System;

/// <summary>
/// Flags cluster nodes that are offline at report time. Beyond the obvious
/// availability impact, an offline node means most checks against that node
/// silently degrade.
/// Mapped by ISO 27001:2022 A.8.14 and NIS2 Art. 21(c).
/// </summary>
internal sealed class OfflineNodesCheck : IComplianceCheck
{
    public string Id => "system.node-offline";
    public string Title => "All cluster nodes should be online";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Nodes];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var nodes = ctx.Get<NodeInfo>(ComplianceDataKind.Nodes);

        foreach (var n in nodes)
        {
            if (n.IsOnline) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.High,
                ScopeType = "node",
                Scope = n.Node,
                Title = "Node offline",
                Details = $"Node {n.Node} is offline at report time.",
                Remediation = "Investigate why the node is offline (hardware, network, services) and bring it back online.",
            };
        }
    }
}
