/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Diagnostics;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    /// <summary>
    /// Writes the Issues section to the report and prepends a stat entry so it shows up
    /// as the second item on the cover. No-op when there are no collected issues.
    /// </summary>
    private void AddIssuesSection(List<SectionStat> stats, Stopwatch sw)
    {
        if (!_issues.HasAny) { return; }

        ReportGlobal("Issues");
        sw.Restart();

        using var section = _writer.AddSection("Issues");
        section.AddTable(null,
                         _issues.All.Select(a => new
                         {
                             Severity = a.Severity.ToString(),
                             a.Section,
                             MessageWrap = a.Message,
                             a.Timestamp,
                             a.LinkKey,
                         }),
                         new TableOptions<dynamic>().WithColumnLink("Section", r => (string?)r.LinkKey));

        stats.Insert(0, new("Issues", "Warnings and errors collected during report generation", _issues.All.Count, sw.Elapsed));
    }
}
