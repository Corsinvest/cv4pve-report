/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Compliance settings — one flag per supported standard. When at least one
/// flag is true the report adds a dedicated compliance section listing findings.
/// </summary>
public class SettingsCompliance
{
    /// <summary>Include ISO/IEC 27001:2022 compliance findings.</summary>
    public bool ISO27001 { get; set; }

    /// <summary>Include NIS2 (EU Directive 2022/2555) compliance findings.</summary>
    public bool NIS2 { get; set; }

    /// <summary>Include CIS Controls v8 compliance findings.</summary>
    public bool CIS { get; set; }

    /// <summary>Include AgID Misure Minime di sicurezza ICT (Italian PA baseline).</summary>
    public bool AgID { get; set; }

    /// <summary>Include PCI DSS v4 compliance findings.</summary>
    public bool PCIDSS { get; set; }

    /// <summary>Include GDPR Art. 32 (technical/organisational measures) compliance findings.</summary>
    public bool GDPR { get; set; }

    /// <summary>Include DORA (EU Regulation 2022/2554) compliance findings.</summary>
    public bool DORA { get; set; }

    /// <summary>Include NIST Cybersecurity Framework 2.0 compliance findings.</summary>
    public bool NISTCSF { get; set; }

    /// <summary>Include ISO/IEC 27017:2015 (cloud security extensions to ISO 27001) findings.</summary>
    public bool ISO27017 { get; set; }

    /// <summary>True when at least one compliance standard is enabled.</summary>
    public bool AnyEnabled => ISO27001 || NIS2 || CIS || AgID || PCIDSS || GDPR || DORA || NISTCSF || ISO27017;
}
