/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Firewall;

/// <summary>
/// Informational: more than 10 disabled firewall rules cluster-wide. Stale
/// disabled rules are noise — periodic cleanup keeps the ruleset auditable.
/// Mapped by ISO 27001:2022 A.8.21.
/// </summary>
internal sealed class ManyDisabledFirewallRulesCheck : IComplianceCheck
{
    private const int Threshold = 10;

    public string Id => "firewall.many-disabled-rules";
    public string Title => "Disabled firewall rules should be cleaned up";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.FirewallRules];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var rules = ctx.Get<FirewallRuleInfo>(ComplianceDataKind.FirewallRules);
        var disabled = rules.Count(r => !r.Enabled);
        if (disabled < Threshold) { yield break; }

        yield return new ComplianceFinding
        {
            CheckId = Id,
            Severity = Severity.Info,
            ScopeType = "cluster",
            Scope = "cluster",
            Title = "Many disabled firewall rules",
            Details = $"{disabled} disabled firewall rules across the cluster — review and delete obsolete ones.",
            Remediation = "Periodically prune disabled rules; keep only rules that are intentionally paused.",
        };
    }
}
