/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Access;

/// <summary>
/// Flags API tokens with no expiration date — long-lived credentials are a credential-leakage risk.
/// Shared across standards: mapped by ISO 27001:2022 A.5.17 and NIS2 Art. 21(j).
/// </summary>
internal sealed class ApiTokenWithoutExpiryCheck : IComplianceCheck
{
    public string Id => "access.api-token-no-expiry";
    public string Title => "API tokens must have an expiration date";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Users];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var users = ctx.Get<UserInfo>(ComplianceDataKind.Users);

        foreach (var user in users)
        {
            foreach (var token in user.Tokens)
            {
                if (token.ExpireUnix is null or 0)
                {
                    yield return new ComplianceFinding
                    {
                        CheckId = Id,
                        Severity = Severity.Medium,
                        ScopeType = "token",
                        Scope = $"{user.Id}!{token.TokenId}",
                        Title = "API token without expiration",
                        Details = $"Token '{token.TokenId}' of user '{user.Id}' has no expiration date.",
                        Remediation = "Set an expiration date in Datacenter → Permissions → API Tokens, or rotate the token periodically.",
                    };
                }
            }
        }
    }
}
