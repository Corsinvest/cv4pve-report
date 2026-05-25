/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Crypto;

/// <summary>
/// Flags node certificates whose <c>notAfter</c> is already in the past.
/// An expired TLS certificate breaks management/cluster traffic — high severity.
/// Mapped by ISO 27001:2022 A.8.24 and NIS2 Art. 21(h).
/// </summary>
internal sealed class CertificateExpiredCheck : IComplianceCheck
{
    public string Id => "crypto.certificate-expired";
    public string Title => "Node TLS certificates must not be expired";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Certificates];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var certs = ctx.Get<CertificateInfo>(ComplianceDataKind.Certificates);
        var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var c in certs)
        {
            if (c.NotAfterUnix == 0) { continue; }
            if (c.NotAfterUnix >= nowUnix) { continue; }

            var expired = DateTimeOffset.FromUnixTimeSeconds(c.NotAfterUnix).UtcDateTime;
            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Critical,
                ScopeType = "certificate",
                Scope = $"{c.Node}/{c.FileName}",
                ScopeName = c.Subject,
                Title = "Expired TLS certificate",
                Details = $"Certificate {c.FileName} on node {c.Node} expired on {expired:yyyy-MM-dd} (subject: {c.Subject}).",
                Remediation = "Renew the certificate immediately (Node → System → Certificates) or recreate via ACME / pveproxy.",
            };
        }
    }
}
