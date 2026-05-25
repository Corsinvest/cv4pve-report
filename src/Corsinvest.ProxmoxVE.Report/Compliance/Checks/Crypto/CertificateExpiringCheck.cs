/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Crypto;

/// <summary>
/// Flags node certificates expiring within the next 30 days. Gives operators
/// enough headroom to schedule a manual renewal or ACME refresh.
/// Mapped by ISO 27001:2022 A.8.24 and NIS2 Art. 21(h).
/// </summary>
internal sealed class CertificateExpiringCheck : IComplianceCheck
{
    private const int WarningDays = 30;

    public string Id => "crypto.certificate-expiring-30d";
    public string Title => "Node TLS certificates should not expire within 30 days";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Certificates];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var certs = ctx.Get<CertificateInfo>(ComplianceDataKind.Certificates);
        var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var thresholdUnix = nowUnix + WarningDays * 86400L;

        foreach (var c in certs)
        {
            if (c.NotAfterUnix == 0) { continue; }
            if (c.NotAfterUnix < nowUnix) { continue; }       // already expired — handled by CertificateExpiredCheck
            if (c.NotAfterUnix >= thresholdUnix) { continue; }

            var expiry = DateTimeOffset.FromUnixTimeSeconds(c.NotAfterUnix).UtcDateTime;
            var daysLeft = (int)Math.Ceiling((c.NotAfterUnix - nowUnix) / 86400d);
            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.High,
                ScopeType = "certificate",
                Scope = $"{c.Node}/{c.FileName}",
                ScopeName = c.Subject,
                Title = "TLS certificate expiring soon",
                Details = $"Certificate {c.FileName} on node {c.Node} expires on {expiry:yyyy-MM-dd} ({daysLeft} day(s) left).",
                Remediation = "Plan certificate renewal in Node → System → Certificates; if ACME-managed, verify the renewal hook.",
            };
        }
    }
}
