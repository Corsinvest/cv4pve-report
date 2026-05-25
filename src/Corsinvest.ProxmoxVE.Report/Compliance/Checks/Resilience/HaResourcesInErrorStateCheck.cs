/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Resilience;

/// <summary>
/// Flags HA resources in a non-healthy state (error, fence, freeze, ignored).
/// These indicate an active HA problem that needs operator attention.
/// Mapped by ISO 27001:2022 A.8.14 and NIS2 Art. 21(c).
/// </summary>
internal sealed class HaResourcesInErrorStateCheck : IComplianceCheck
{
    private static readonly string[] _badStates = ["error", "fence", "freeze", "ignored", "disabled"];

    public string Id => "resilience.ha-resource-bad-state";
    public string Title => "HA resources should be in a healthy state";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.HaResources];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var ha = ctx.Get<HaResourceInfo>(ComplianceDataKind.HaResources);

        foreach (var h in ha)
        {
            var state = (h.State ?? "").ToLowerInvariant();
            if (!_badStates.Contains(state)) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = state == "disabled" ? Severity.Info : Severity.High,
                ScopeType = "ha-resource",
                Scope = h.Sid,
                Title = $"HA resource in '{state}' state",
                Details = $"HA resource '{h.Sid}' is in state '{state}'.",
                Remediation = "Investigate via Datacenter → HA → Status; resolve underlying issue or re-enable the resource.",
            };
        }
    }
}
