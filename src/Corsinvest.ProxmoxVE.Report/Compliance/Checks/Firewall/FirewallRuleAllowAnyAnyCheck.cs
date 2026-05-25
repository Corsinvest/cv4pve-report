/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Firewall;

/// <summary>
/// Flags enabled IN rules whose Action is ACCEPT and that have no source AND no
/// destination AND no macro (i.e. "allow any → any"). Such rules defeat the
/// firewall: they let everything through regardless of the default policy.
/// Mapped by ISO 27001:2022 A.8.21 and NIS2 Art. 21(e).
/// </summary>
internal sealed class FirewallRuleAllowAnyAnyCheck : IComplianceCheck
{
    public string Id => "firewall.rule-allow-any-any";
    public string Title => "Enabled firewall rules should not allow any → any traffic";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.FirewallRules];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var rules = ctx.Get<FirewallRuleInfo>(ComplianceDataKind.FirewallRules);

        foreach (var r in rules)
        {
            if (!r.Enabled) { continue; }
            if (!r.Type.Equals("in", StringComparison.OrdinalIgnoreCase)) { continue; }
            if (!r.Action.Equals("ACCEPT", StringComparison.OrdinalIgnoreCase)) { continue; }
            if (!string.IsNullOrWhiteSpace(r.Source)) { continue; }
            if (!string.IsNullOrWhiteSpace(r.Dest)) { continue; }
            if (!string.IsNullOrWhiteSpace(r.Macro)) { continue; }

            var scopeLabel = string.IsNullOrEmpty(r.ScopeName)
                                ? r.Scope
                                : $"{r.Scope} ({r.ScopeName})";

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.High,
                ScopeType = r.ScopeType,
                Scope = r.Scope,
                ScopeName = r.ScopeName,
                Title = "Permissive firewall rule (allow any → any)",
                Details = $"Rule #{r.Position} on {r.ScopeType} '{scopeLabel}' is enabled and accepts any IN traffic.",
                Remediation = "Narrow the rule with a specific source/destination/macro, or disable it.",
            };
        }
    }
}
