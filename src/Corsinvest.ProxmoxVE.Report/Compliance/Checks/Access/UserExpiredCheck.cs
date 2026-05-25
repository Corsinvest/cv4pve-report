/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Access;

/// <summary>
/// Flags enabled users whose expiration date is in the past — the account should
/// have been deactivated but is still usable.
/// Shared across standards: ISO 27001:2022 A.5.16 / A.5.18, NIS2 Art. 21(i).
/// </summary>
internal sealed class UserExpiredCheck : IComplianceCheck
{
    public string Id => "access.user-expired-still-enabled";
    public string Title => "Enabled users with expired account";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Users];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var users = ctx.Get<UserInfo>(ComplianceDataKind.Users);
        var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var u in users)
        {
            if (!u.Enabled) { continue; }
            if (u.ExpireUnix is not > 0) { continue; }
            if (u.ExpireUnix.Value >= nowUnix) { continue; }

            var expired = DateTimeOffset.FromUnixTimeSeconds(u.ExpireUnix.Value).UtcDateTime;
            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Medium,
                ScopeType = "user",
                Scope = u.Id,
                Title = "Enabled user with expired account",
                Details = $"User '{u.Id}' is enabled but expired on {expired:yyyy-MM-dd}.",
                Remediation = "Disable the user or update its expiration date in Datacenter → Permissions → Users.",
            };
        }
    }
}
