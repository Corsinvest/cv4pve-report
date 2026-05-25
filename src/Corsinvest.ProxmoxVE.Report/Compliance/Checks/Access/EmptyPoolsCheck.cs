/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Access;

/// <summary>
/// Informational: resource pools without any member. Empty pools indicate
/// forgotten cleanup after decommission/migration.
/// Mapped by ISO 27001:2022 A.5.18.
/// </summary>
internal sealed class EmptyPoolsCheck : IComplianceCheck
{
    public string Id => "access.empty-pool";
    public string Title => "Empty resource pools should be cleaned up";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Pools];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var pools = ctx.Get<PoolInfo>(ComplianceDataKind.Pools);

        foreach (var p in pools)
        {
            if (p.MemberCount > 0) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Info,
                ScopeType = "pool",
                Scope = p.Id,
                Title = "Empty resource pool",
                Details = $"Pool '{p.Id}' has no members.",
                Remediation = "Delete the pool if no longer needed, or document why it exists empty.",
            };
        }
    }
}
