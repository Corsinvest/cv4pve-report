/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.System;

/// <summary>
/// Flags nodes whose Proxmox subscription is missing or no longer Active.
/// An expired subscription blocks access to the enterprise repo — patches stop
/// landing and the host drifts away from compliance.
/// Mapped by ISO 27001:2022 A.5.20 / A.8.8 and NIS2 Art. 21(e).
/// </summary>
internal sealed class NodesWithExpiredSubscriptionCheck : IComplianceCheck
{
    public string Id => "system.node-subscription-not-active";
    public string Title => "All cluster nodes should have an active Proxmox subscription";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Nodes];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var nodes = ctx.Get<NodeInfo>(ComplianceDataKind.Nodes);

        foreach (var n in nodes)
        {
            if (!n.IsOnline) { continue; }

            var status = n.SubscriptionStatus ?? "";
            if (status.Equals("Active", StringComparison.OrdinalIgnoreCase)) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Medium,
                ScopeType = "node",
                Scope = n.Node,
                Title = "Node subscription not active",
                Details = string.IsNullOrEmpty(status)
                            ? $"Node {n.Node} has no subscription information."
                            : $"Node {n.Node} subscription status is '{status}'" + (string.IsNullOrEmpty(n.SubscriptionNextDueDate) ? "." : $" (next due {n.SubscriptionNextDueDate})."),
                Remediation = "Activate or renew the subscription in Node → Subscription, or move the node to the no-subscription repo if intentional.",
            };
        }
    }
}
