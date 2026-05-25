/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    /// <summary>
    /// Compliance section: one overview page with links to per-standard pages, plus one
    /// dedicated page per enabled pack (Controls + Findings). Section is skipped silently
    /// when no standard is enabled in <see cref="SettingsCompliance"/>.
    /// </summary>
    private Task<int> AddComplianceDataAsync()
    {
        if (!_compliance.AnyEnabled) { return Task.FromResult(0); }

        ReportGlobal("Compliance: Running checks");
        var reports = _compliance.Run();

        // Pack pages first so the XLSX sheet names exist when the overview resolves Pack hyperlinks.
        var totalFindings = 0;
        foreach (var report in reports)
        {
            totalFindings += WriteCompliancePackPage(report);
        }

        WriteComplianceOverview(reports);

        return Task.FromResult(totalFindings);
    }

    private record ComplianceOverviewRow(string Pack,
                                         string Title,
                                         int Controls,
                                         int Findings,
                                         int Critical,
                                         int High,
                                         int Medium,
                                         int Low,
                                         int Info,
                                         int Skipped,
                                         double ScoreBadge);

    private void WriteComplianceOverview(IReadOnlyList<ComplianceReport> reports)
    {
        using var sw = _writer.AddSection("Compliance");

        var rows = reports.Select(r => new ComplianceOverviewRow(
            Pack: r.PackId,
            Title: r.PackTitle,
            Controls: r.Controls.Count,
            Findings: r.Controls.Sum(c => c.Findings.Count),
            Critical: CountBy(r, Severity.Critical),
            High: CountBy(r, Severity.High),
            Medium: CountBy(r, Severity.Medium),
            Low: CountBy(r, Severity.Low),
            Info: CountBy(r, Severity.Info),
            Skipped: r.Controls.Sum(c => c.SkippedCheckIds.Count),
            ScoreBadge: PackScore(r))).ToList();

        sw.AddTable("Standards",
                    rows,
                    new TableOptions<ComplianceOverviewRow>().WithColumnLink("Pack", row => LinkKey.CompliancePack(row.Pack)));
    }

    private int WriteCompliancePackPage(ComplianceReport report)
    {
        using var sw = _writer.AddSection(new SectionId.Compliance(report.PackId, report.PackTitle));

        sw.AddBackLink("Compliance overview", LinkKey.Compliance);

        sw.AddKeyValue($"{report.PackId} - {report.PackTitle}", new Dictionary<string, object?>
        {
            ["Controls"] = report.Controls.Count,
            ["Findings"] = report.Controls.Sum(c => c.Findings.Count),
            ["Skipped"] = report.Controls.Sum(c => c.SkippedCheckIds.Count),
            ["Score"] = $"{PackScore(report) * 100:F0}%",
        });

        sw.AddKeyValue("Disclaimer", new Dictionary<string, object?>
        {
            ["ScopeWrap"] = "Automated technical assessment based on Proxmox VE state at report time. Procedural, organisational and physical controls of the standard are out of scope and require manual review.",
            ["NotaBeneWrap"] = "This report does NOT constitute formal certification or attestation. Use it as a continuous self-assessment input alongside your audit programme.",
        });

        var controlRows = report.Controls.Select(c => new
        {
            Control = c.ControlId,
            c.Title,
            Status = ControlStatus(c),
            MaxSeverity = MaxSeverityLabel(c),
            ScoreBadge = ControlScore(c),
            Checks = c.TotalChecks,
            Findings = c.Findings.Count,
            Skipped = c.SkippedCheckIds.Count,
        }).ToList();

        sw.AddTable("Controls", controlRows);

        var checkRows = report.Controls.SelectMany(BuildCheckRows).ToList();
        sw.AddTable("Checks", checkRows);

        return report.Controls.Sum(c => c.Findings.Count);
    }

    private static IEnumerable<object> BuildCheckRows(ControlReport c)
    {
        var findingsByCheck = c.Findings.GroupBy(f => f.CheckId)
                                        .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var outcome in c.Outcomes)
        {
            if (!outcome.Executed)
            {
                yield return new
                {
                    Control = c.ControlId,
                    Status = "N/A",
                    Severity = "—",
                    CheckId = outcome.CheckId,
                    ScopeType = "—",
                    Scope = "—",
                    ScopeName = "—",
                    Title = outcome.Title,
                    DetailsWrap = "Data unavailable: required inputs not provided by the report.",
                    RemediationWrap = "—",
                };
                continue;
            }

            if (!findingsByCheck.TryGetValue(outcome.CheckId, out var fs))
            {
                yield return new
                {
                    Control = c.ControlId,
                    Status = "PASS",
                    Severity = "—",
                    CheckId = outcome.CheckId,
                    ScopeType = "—",
                    Scope = "—",
                    ScopeName = "—",
                    Title = outcome.Title,
                    DetailsWrap = "No issues detected.",
                    RemediationWrap = "—",
                };
                continue;
            }

            foreach (var f in fs)
            {
                yield return new
                {
                    Control = c.ControlId,
                    Status = "FAIL",
                    Severity = f.Severity.ToString(),
                    CheckId = f.CheckId,
                    ScopeType = Dash(f.ScopeType),
                    Scope = Dash(f.Scope),
                    ScopeName = Dash(f.ScopeName),
                    Title = f.Title,
                    DetailsWrap = Dash(f.Details),
                    RemediationWrap = Dash(f.Remediation),
                };
            }
        }
    }

    private static int CountBy(ComplianceReport report, Severity severity)
        => report.Controls.Sum(c => c.Findings.Count(f => f.Severity == severity));

    /// <summary>Empty cells render as a dash so the row reads consistently in HTML/XLSX.</summary>
    private static string Dash(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    /// <summary>Highest finding severity in a control, or "—" when there are no findings.</summary>
    private static string MaxSeverityLabel(ControlReport c)
        => c.Findings.Count == 0
            ? "—"
            : c.Findings.Max(f => f.Severity).ToString();

    /// <summary>
    /// PASS  — all checks ran and produced no finding.
    /// FAIL  — at least one finding.
    /// N/A   — no check was runnable (all skipped: data unavailable).
    /// PARTIAL — some checks ran (all passed) and some were skipped.
    /// </summary>
    private static string ControlStatus(ControlReport c)
    {
        if (c.Findings.Count > 0) { return "FAIL"; }
        if (c.SkippedCheckIds.Count == c.TotalChecks) { return "N/A"; }
        if (c.SkippedCheckIds.Count > 0) { return "PARTIAL"; }
        return "PASS";
    }

    /// <summary>
    /// % of executed checks that passed within a control. Returns 0 when no check ran (N/A).
    /// Stored as a fraction (0..1) because the column suffix "Pct" formats it as percentage.
    /// </summary>
    private static double ControlScore(ControlReport c)
    {
        var executed = c.TotalChecks - c.SkippedCheckIds.Count;
        if (executed <= 0) { return 0d; }
        var failed = c.Findings.Select(f => f.CheckId).Distinct().Count();
        return (executed - failed) / (double)executed;
    }

    /// <summary>Average control score across non-N/A controls. Returns 0 when no control is executable.</summary>
    private static double PackScore(ComplianceReport r)
    {
        var executable = r.Controls.Where(c => c.SkippedCheckIds.Count < c.TotalChecks).ToList();
        if (executable.Count == 0) { return 0d; }
        return executable.Average(ControlScore);
    }
}
