/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Backup;

/// <summary>
/// Flags enabled backup jobs without a schedule — a backup that never runs has
/// the same compliance value as no backup at all.
/// Mapped by ISO 27001:2022 A.8.13 and NIS2 Art. 21(c).
/// </summary>
internal sealed class BackupJobsWithoutScheduleCheck : IComplianceCheck
{
    public string Id => "backup.job-without-schedule";
    public string Title => "Enabled backup jobs must have a schedule";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.BackupJobs];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var jobs = ctx.Get<BackupJobInfo>(ComplianceDataKind.BackupJobs);

        foreach (var j in jobs)
        {
            if (!j.Enabled) { continue; }
            if (!string.IsNullOrWhiteSpace(j.Schedule)) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.High,
                ScopeType = "backup-job",
                Scope = j.Id,
                Title = "Backup job without schedule",
                Details = $"Backup job '{j.Id}' is enabled but has no schedule defined — it will never run automatically.",
                Remediation = "Define a schedule for the job in Datacenter → Backup → Edit, or disable the job if not needed.",
            };
        }
    }
}
