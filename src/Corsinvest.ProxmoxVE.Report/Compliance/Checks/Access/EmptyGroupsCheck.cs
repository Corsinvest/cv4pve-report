/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Access;

/// <summary>
/// Informational: empty groups. They are not a security issue per se, but they
/// usually signal forgotten cleanup or stale onboarding/offboarding artifacts.
/// Mapped by ISO 27001:2022 A.5.18.
/// </summary>
internal sealed class EmptyGroupsCheck : IComplianceCheck
{
    public string Id => "access.empty-group";
    public string Title => "Empty groups should be cleaned up";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Groups];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var groups = ctx.Get<GroupInfo>(ComplianceDataKind.Groups);

        foreach (var g in groups)
        {
            if (g.Users.Count > 0) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Info,
                ScopeType = "group",
                Scope = g.Id,
                Title = "Empty group",
                Details = $"Group '{g.Id}' has no members.",
                Remediation = "Delete the group if no longer needed, or document its purpose.",
            };
        }
    }
}
