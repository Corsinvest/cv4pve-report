/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Access;

/// <summary>
/// Informational: custom roles defined but never referenced in ACL. Built-in
/// roles (NoAccess, PVEAdmin, ...) are ignored.
/// Mapped by ISO 27001:2022 A.5.18.
/// </summary>
internal sealed class UnusedCustomRolesCheck : IComplianceCheck
{
    public string Id => "access.unused-custom-role";
    public string Title => "Custom roles not used by any ACL should be removed";

    public IReadOnlyList<ComplianceDataKind> Requires =>
    [
        ComplianceDataKind.Roles,
        ComplianceDataKind.Acl,
    ];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var roles = ctx.Get<RoleInfo>(ComplianceDataKind.Roles);
        var acl = ctx.Get<AclEntry>(ComplianceDataKind.Acl);

        var usedRoles = acl.Select(a => a.RoleId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var r in roles)
        {
            if (r.IsBuiltIn) { continue; }
            if (usedRoles.Contains(r.Id)) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Info,
                ScopeType = "role",
                Scope = r.Id,
                Title = "Unused custom role",
                Details = $"Custom role '{r.Id}' is not referenced by any ACL entry.",
                Remediation = "Delete the role if obsolete, or assign it where appropriate.",
            };
        }
    }
}
