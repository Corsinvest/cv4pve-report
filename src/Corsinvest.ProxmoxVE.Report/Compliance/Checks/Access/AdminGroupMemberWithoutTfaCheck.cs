/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Access;

/// <summary>
/// Same intent as <see cref="AdminWithoutTfaCheck"/>, but for users that inherit
/// Administrator/PVEAdmin via group membership rather than a direct ACL entry.
/// Shared across standards: ISO 27001:2022 A.5.17 / A.8.5, NIS2 Art. 21(j).
/// </summary>
internal sealed class AdminGroupMemberWithoutTfaCheck : IComplianceCheck
{
    private static readonly string[] _privilegedRoles = ["Administrator", "PVEAdmin"];

    public string Id => "access.admin-group-member-no-tfa";
    public string Title => "Members of admin groups must have two-factor authentication enabled";

    public IReadOnlyList<ComplianceDataKind> Requires =>
    [
        ComplianceDataKind.Users,
        ComplianceDataKind.Tfa,
        ComplianceDataKind.Acl,
        ComplianceDataKind.Groups,
    ];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var users = ctx.Get<UserInfo>(ComplianceDataKind.Users);
        var tfa = ctx.Get<TfaInfo>(ComplianceDataKind.Tfa);
        var acl = ctx.Get<AclEntry>(ComplianceDataKind.Acl);
        var groups = ctx.Get<GroupInfo>(ComplianceDataKind.Groups);

        var usersWithTfa = tfa.Where(t => t.Types.Count > 0)
                              .Select(t => t.UserId)
                              .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var privilegedGroupIds = acl.Where(a => a.Path == "/"
                                              && a.Type.Equals("group", StringComparison.OrdinalIgnoreCase)
                                              && _privilegedRoles.Contains(a.RoleId))
                                    .Select(a => a.UserOrGroup)
                                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (privilegedGroupIds.Count == 0) { yield break; }

        var userById = users.ToDictionary(u => u.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var g in groups)
        {
            if (!privilegedGroupIds.Contains(g.Id)) { continue; }

            foreach (var member in g.Users)
            {
                if (usersWithTfa.Contains(member)) { continue; }
                if (userById.TryGetValue(member, out var u) && !u.Enabled) { continue; }

                yield return new ComplianceFinding
                {
                    CheckId = Id,
                    Severity = Severity.High,
                    ScopeType = "user",
                    Scope = member,
                    Title = "Admin group member without 2FA",
                    Details = $"User '{member}' inherits Administrator/PVEAdmin via group '{g.Id}' but has no TFA configured.",
                    Remediation = "Enable TOTP or WebAuthn for this user in Datacenter → Permissions → Two Factor, or remove from the admin group.",
                };
            }
        }
    }
}
