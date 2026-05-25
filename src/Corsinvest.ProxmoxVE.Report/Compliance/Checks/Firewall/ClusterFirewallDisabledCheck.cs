/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Firewall;

/// <summary>
/// Flags clusters whose datacenter-level firewall is disabled. With the cluster
/// firewall off, per-node and per-VM rules are not enforced — the whole layered
/// security model collapses.
/// Mapped by ISO 27001:2022 A.8.20 and NIS2 Art. 21(e).
/// </summary>
internal sealed class ClusterFirewallDisabledCheck : IComplianceCheck
{
    public string Id => "firewall.cluster-disabled";
    public string Title => "Datacenter firewall must be enabled";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.FirewallClusterOptions];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var opts = ctx.Get<FirewallClusterOptionsInfo>(ComplianceDataKind.FirewallClusterOptions);
        var fw = opts.FirstOrDefault();
        if (fw is null) { yield break; }
        if (fw.Enabled) { yield break; }

        yield return new ComplianceFinding
        {
            CheckId = Id,
            Severity = Severity.High,
            ScopeType = "cluster",
            Scope = "cluster",
            Title = "Datacenter firewall disabled",
            Details = "The datacenter-level firewall is disabled — node and VM firewall rules are not enforced.",
            Remediation = "Enable the firewall in Datacenter → Firewall → Options → Firewall: Yes.",
        };
    }
}
