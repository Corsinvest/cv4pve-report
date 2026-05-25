/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Access;

/// <summary>
/// Flags enabled users without an email address. Security-relevant notifications
/// (backup failures, certificate expiry, HA events) won't reach them.
/// Mapped by ISO 27001:2022 A.5.16 and NIS2 Art. 21(i).
/// </summary>
internal sealed class UsersWithoutEmailCheck : IComplianceCheck
{
    public string Id => "access.user-without-email";
    public string Title => "Enabled users should have an email address";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Users];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var users = ctx.Get<UserInfo>(ComplianceDataKind.Users);

        foreach (var u in users)
        {
            if (!u.Enabled) { continue; }
            if (!string.IsNullOrWhiteSpace(u.Email)) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Low,
                ScopeType = "user",
                Scope = u.Id,
                Title = "User without email",
                Details = $"User '{u.Id}' is enabled but has no email address configured.",
                Remediation = "Set a valid email in Datacenter → Permissions → Users → Edit.",
            };
        }
    }
}
