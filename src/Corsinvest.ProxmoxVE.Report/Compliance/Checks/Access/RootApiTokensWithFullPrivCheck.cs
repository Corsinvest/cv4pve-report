/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Access;

/// <summary>
/// Flags API tokens belonging to root@pam that are NOT privilege-separated.
/// A non-separated root token inherits full root privileges — equivalent to a
/// leaked root password if the token is exposed.
/// Shared across standards: ISO 27001:2022 A.5.17 / A.8.2, NIS2 Art. 21(i).
/// </summary>
internal sealed class RootApiTokensWithFullPrivCheck : IComplianceCheck
{
    public string Id => "access.root-token-not-priv-separated";
    public string Title => "root@pam API tokens should be privilege-separated";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Users];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var users = ctx.Get<UserInfo>(ComplianceDataKind.Users);

        var root = users.FirstOrDefault(u => u.Id.Equals("root@pam", StringComparison.OrdinalIgnoreCase));
        if (root is null) { yield break; }

        foreach (var token in root.Tokens)
        {
            if (token.PrivSeparated) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.High,
                ScopeType = "token",
                Scope = $"root@pam!{token.TokenId}",
                Title = "Non privilege-separated root token",
                Details = $"Token 'root@pam!{token.TokenId}' is NOT privilege-separated — it inherits full root permissions.",
                Remediation = "Recreate the token with privsep=1 and grant only the strictly-needed ACL permissions to the token id.",
            };
        }
    }
}
