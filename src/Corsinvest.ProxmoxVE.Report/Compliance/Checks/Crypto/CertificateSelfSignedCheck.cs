/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Crypto;

/// <summary>
/// Informational: certificates where Subject == Issuer (self-signed). Default
/// for fresh Proxmox installs but auditors expect production clusters to use a
/// CA-issued or ACME-managed certificate.
/// Mapped by ISO 27001:2022 A.8.24 and NIS2 Art. 21(h).
/// </summary>
internal sealed class CertificateSelfSignedCheck : IComplianceCheck
{
    public string Id => "crypto.certificate-self-signed";
    public string Title => "Production node certificates should be issued by a trusted CA";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Certificates];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var certs = ctx.Get<CertificateInfo>(ComplianceDataKind.Certificates);

        foreach (var c in certs)
        {
            if (string.IsNullOrWhiteSpace(c.Subject) || string.IsNullOrWhiteSpace(c.Issuer)) { continue; }
            if (!c.Subject.Equals(c.Issuer, StringComparison.Ordinal)) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Low,
                ScopeType = "certificate",
                Scope = $"{c.Node}/{c.FileName}",
                ScopeName = c.Subject,
                Title = "Self-signed certificate",
                Details = $"Certificate {c.FileName} on node {c.Node} is self-signed (Subject == Issuer).",
                Remediation = "Replace with a CA-issued certificate or configure ACME (Node → System → Certificates).",
            };
        }
    }
}
