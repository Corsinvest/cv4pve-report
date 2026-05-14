/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Output format for the generated report.
/// </summary>
public enum ReportFormat
{
    /// <summary>Single Excel workbook (.xlsx).</summary>
    Xlsx,

    /// <summary>Static HTML site (index.html + per-section pages), packaged as a .zip.</summary>
    Html,

    /// <summary>Multi-file JSON (one file per section + per-resource detail files), packaged as a .zip.</summary>
    Json,
}
