/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Access;

/// <summary>
/// Flags users that hold Administrator/PVEAdmin role on path "/" but have no TFA entry.
/// Shared across standards: mapped by ISO 27001:2022 A.5.17 / A.8.5 and NIS2 Art. 21(j).
/// </summary>
internal sealed class AdminWithoutTfaCheck : IComplianceCheck
{
    private static readonly string[] _privilegedRoles = ["Administrator", "PVEAdmin"];

    public string Id => "access.admin-no-tfa";
    public string Title => "Administrator users must have two-factor authentication enabled";

    public IReadOnlyList<ComplianceDataKind> Requires =>
    [
        ComplianceDataKind.Users,
        ComplianceDataKind.Tfa,
        ComplianceDataKind.Acl,
    ];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var users = ctx.Get<UserInfo>(ComplianceDataKind.Users);
        var tfa = ctx.Get<TfaInfo>(ComplianceDataKind.Tfa);
        var acl = ctx.Get<AclEntry>(ComplianceDataKind.Acl);

        var usersWithTfa = tfa.Where(t => t.Types.Count > 0)
                              .Select(t => t.UserId)
                              .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var privilegedUsers = acl.Where(a => a.Path == "/"
                                          && a.Type.Equals("user", StringComparison.OrdinalIgnoreCase)
                                          && _privilegedRoles.Contains(a.RoleId))
                                 .Select(a => a.UserOrGroup)
                                 .Distinct(StringComparer.OrdinalIgnoreCase);

        var userById = users.ToDictionary(u => u.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var userId in privilegedUsers)
        {
            if (usersWithTfa.Contains(userId)) { continue; }
            if (userById.TryGetValue(userId, out var u) && !u.Enabled) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.High,
                ScopeType = "user",
                Scope = userId,
                Title = "Administrator without 2FA",
                Details = $"User '{userId}' has Administrator/PVEAdmin role on path '/' but no TFA configured.",
                Remediation = "Enable TOTP or WebAuthn for this user in Datacenter → Permissions → Two Factor.",
            };
        }
    }
}
