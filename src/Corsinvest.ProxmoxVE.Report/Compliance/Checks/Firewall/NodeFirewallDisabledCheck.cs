/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Firewall;

/// <summary>
/// Flags nodes whose node-level firewall is disabled. Even with the datacenter
/// firewall on, node-level rules add an extra defense layer; leaving them off
/// weakens segmentation.
/// Mapped by ISO 27001:2022 A.8.20 and NIS2 Art. 21(e).
/// </summary>
internal sealed class NodeFirewallDisabledCheck : IComplianceCheck
{
    public string Id => "firewall.node-disabled";
    public string Title => "Node firewall should be enabled on every node";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.FirewallNodeOptions];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var options = ctx.Get<FirewallNodeOptionsInfo>(ComplianceDataKind.FirewallNodeOptions);

        foreach (var o in options)
        {
            if (o.Enabled) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Medium,
                ScopeType = "node",
                Scope = o.Node,
                Title = "Node firewall disabled",
                Details = $"Node {o.Node} has the node-level firewall disabled.",
                Remediation = "Enable the firewall in Node → Firewall → Options → Firewall: Yes.",
            };
        }
    }
}
