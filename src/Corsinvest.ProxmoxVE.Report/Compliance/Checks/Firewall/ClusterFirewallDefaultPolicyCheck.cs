/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Firewall;

/// <summary>
/// Flags clusters whose default input policy is ACCEPT. Defense-in-depth requires
/// an implicit-deny posture (DROP or REJECT) so allow-rules are explicit.
/// Only emitted when the cluster firewall is actually enabled.
/// Mapped by ISO 27001:2022 A.8.21 and NIS2 Art. 21(e).
/// </summary>
internal sealed class ClusterFirewallDefaultPolicyCheck : IComplianceCheck
{
    public string Id => "firewall.cluster-default-policy-accept";
    public string Title => "Datacenter firewall default input policy should be DROP or REJECT";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.FirewallClusterOptions];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var fw = ctx.Get<FirewallClusterOptionsInfo>(ComplianceDataKind.FirewallClusterOptions).FirstOrDefault();
        if (fw is null) { yield break; }
        if (!fw.Enabled) { yield break; }

        var policy = (fw.PolicyIn ?? "ACCEPT").ToUpperInvariant();
        if (policy is "DROP" or "REJECT") { yield break; }

        yield return new ComplianceFinding
        {
            CheckId = Id,
            Severity = Severity.High,
            ScopeType = "cluster",
            Scope = "cluster",
            Title = "Permissive default input policy",
            Details = $"Datacenter firewall input policy is '{policy}' — anything not matched by a rule is allowed.",
            Remediation = "Set Datacenter → Firewall → Options → Input policy to DROP (or REJECT) and add explicit allow rules.",
        };
    }
}
