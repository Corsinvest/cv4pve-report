/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Writers;

/// <summary>
/// Single classification used by every writer to format a column.
/// Captures both the naming convention (suffix) and the rendering intent
/// (alignment, cell format, JSON suffix-stripping).
/// </summary>
internal enum ColumnKind
{
    Text,
    Number,
    DateTime,
    DateOnly,
    Percentage,
    GB,
    MB,
    Flag,
    Wrap,
    HealthScore,
    StatusBadge,
    ScoreBadge,
}
