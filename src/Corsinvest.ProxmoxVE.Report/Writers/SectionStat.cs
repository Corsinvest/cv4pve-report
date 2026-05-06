/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Writers;

/// <summary>
/// Per-section statistics collected by ReportEngine and passed to the writer
/// for the cover/summary page (row count and elapsed time).
/// </summary>
internal sealed record SectionStat(string Name, int Count, TimeSpan Duration);
