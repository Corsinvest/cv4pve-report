/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Access;

/// <summary>
/// Informational: lists how many users hold Administrator on path "/". Auditors
/// expect "few-as-possible" admins (principle of least privilege).
/// Mapped by ISO 27001:2022 A.8.2 and NIS2 Art. 21(i).
/// </summary>
internal sealed class MultipleAdminUsersCheck : IComplianceCheck
{
    private const int Threshold = 3;

    public string Id => "access.too-many-admin-users";
    public string Title => "Administrator role should be granted to as few users as possible";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Acl];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var acl = ctx.Get<AclEntry>(ComplianceDataKind.Acl);

        var admins = acl.Where(a => a.Path == "/"
                                 && a.Type.Equals("user", StringComparison.OrdinalIgnoreCase)
                                 && a.RoleId == "Administrator")
                        .Select(a => a.UserOrGroup)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

        if (admins.Count < Threshold) { yield break; }

        yield return new ComplianceFinding
        {
            CheckId = Id,
            Severity = Severity.Low,
            ScopeType = "cluster",
            Scope = "cluster",
            Title = "Too many Administrator users",
            Details = $"{admins.Count} users hold the Administrator role on path '/': {string.Join(", ", admins)}.",
            Remediation = "Reduce the number of direct administrators; prefer least-privilege role assignments or admin groups.",
        };
    }
}
