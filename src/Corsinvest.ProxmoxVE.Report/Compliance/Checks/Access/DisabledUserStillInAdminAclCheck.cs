/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Access;

/// <summary>
/// Flags disabled users that still hold an Administrator/PVEAdmin ACL entry on "/".
/// Even if disabled today, the entry will resurface on re-enable — leftover
/// privilege is a leaver-process gap.
/// Mapped by ISO 27001:2022 A.5.18 and NIS2 Art. 21(i).
/// </summary>
internal sealed class DisabledUserStillInAdminAclCheck : IComplianceCheck
{
    private static readonly string[] _privilegedRoles = ["Administrator", "PVEAdmin"];

    public string Id => "access.disabled-user-still-admin";
    public string Title => "Disabled users should not retain Administrator ACL entries";

    public IReadOnlyList<ComplianceDataKind> Requires =>
    [
        ComplianceDataKind.Users,
        ComplianceDataKind.Acl,
    ];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var users = ctx.Get<UserInfo>(ComplianceDataKind.Users);
        var acl = ctx.Get<AclEntry>(ComplianceDataKind.Acl);

        var disabledIds = users.Where(u => !u.Enabled)
                               .Select(u => u.Id)
                               .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var leftover = acl.Where(a => a.Path == "/"
                                   && a.Type.Equals("user", StringComparison.OrdinalIgnoreCase)
                                   && _privilegedRoles.Contains(a.RoleId)
                                   && disabledIds.Contains(a.UserOrGroup));

        foreach (var entry in leftover)
        {
            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Medium,
                ScopeType = "user",
                Scope = entry.UserOrGroup,
                Title = "Disabled user still in admin ACL",
                Details = $"User '{entry.UserOrGroup}' is disabled but still has role '{entry.RoleId}' on '/'.",
                Remediation = "Remove the leftover ACL entry, or delete the user entirely.",
            };
        }
    }
}
