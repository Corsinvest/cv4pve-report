/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Firewall;

/// <summary>
/// Cluster-level finding: cluster has enabled firewall rules but none of them
/// is configured to log matched traffic. Without logging there is no forensic
/// record for the auditor when investigating an incident.
/// Mapped by ISO 27001:2022 A.8.15 and NIS2 Art. 21(b).
/// </summary>
internal sealed class FirewallRuleNoLoggingCheck : IComplianceCheck
{
    public string Id => "firewall.no-logging-rules";
    public string Title => "At least one enabled firewall rule should have logging on";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.FirewallRules];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var rules = ctx.Get<FirewallRuleInfo>(ComplianceDataKind.FirewallRules);
        var enabled = rules.Where(r => r.Enabled).ToList();
        if (enabled.Count == 0) { yield break; }

        if (enabled.Any(r => HasLogging(r.Log))) { yield break; }

        yield return new ComplianceFinding
        {
            CheckId = Id,
            Severity = Severity.Low,
            ScopeType = "cluster",
            Scope = "cluster",
            Title = "No firewall rules with logging",
            Details = $"{enabled.Count} enabled firewall rule(s) found, none with logging configured.",
            Remediation = "Enable logging on at least the most security-relevant rules (Datacenter → Firewall → rule → Log).",
        };
    }

    private static bool HasLogging(string? log)
    {
        if (string.IsNullOrWhiteSpace(log)) { return false; }
        return !log.Equals("nolog", StringComparison.OrdinalIgnoreCase);
    }
}
