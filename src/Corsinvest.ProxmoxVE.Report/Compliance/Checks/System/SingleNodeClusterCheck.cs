/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.System;

/// <summary>
/// Informational: cluster has a single node. HA, replication and quorum are
/// effectively unavailable — auditors should know the deployment lacks
/// horizontal redundancy.
/// Mapped by ISO 27001:2022 A.8.14 and NIS2 Art. 21(c).
/// </summary>
internal sealed class SingleNodeClusterCheck : IComplianceCheck
{
    public string Id => "system.single-node-cluster";
    public string Title => "Production clusters should have more than one node";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Nodes];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var nodes = ctx.Get<NodeInfo>(ComplianceDataKind.Nodes);

        if (nodes.Count > 1) { yield break; }
        if (nodes.Count == 0) { yield break; }

        yield return new ComplianceFinding
        {
            CheckId = Id,
            Severity = Severity.Low,
            ScopeType = "cluster",
            Scope = "cluster",
            Title = "Single-node cluster",
            Details = "The cluster has a single node — HA, replication and quorum are not effective.",
            Remediation = "Add at least two more nodes to form a redundant cluster (3+ nodes required for healthy quorum).",
        };
    }
}
