/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Access;

/// <summary>
/// Flags authentication realms that don't enforce TFA at the realm level. With
/// realm-level TFA every user that authenticates against the realm is forced to
/// provide a second factor, regardless of per-user configuration.
/// Shared across standards: ISO 27001:2022 A.5.17 / A.8.5, NIS2 Art. 21(j).
/// </summary>
internal sealed class WeakRealmTfaCheck : IComplianceCheck
{
    public string Id => "access.realm-without-tfa";
    public string Title => "Authentication realms should enforce TFA at realm level";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Domains];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var domains = ctx.Get<DomainInfo>(ComplianceDataKind.Domains);

        foreach (var d in domains)
        {
            if (!string.IsNullOrWhiteSpace(d.Tfa)) { continue; }
            // pve/pam realms can rely on per-user TFA — only warn on external realms (ad, ldap, openid).
            var isExternal = d.Type.Equals("ad", StringComparison.OrdinalIgnoreCase)
                          || d.Type.Equals("ldap", StringComparison.OrdinalIgnoreCase)
                          || d.Type.Equals("openid", StringComparison.OrdinalIgnoreCase);
            if (!isExternal) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Medium,
                ScopeType = "realm",
                Scope = d.Realm,
                Title = "External realm without realm-level TFA",
                Details = $"Realm '{d.Realm}' (type {d.Type}) does not enforce TFA at realm level.",
                Remediation = "Configure realm-level TFA in Datacenter → Permissions → Realms → Edit → TFA.",
            };
        }
    }
}
