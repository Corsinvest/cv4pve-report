/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Backup;

/// <summary>
/// Informational: backup jobs currently disabled. Auditors want to know about
/// jobs that exist but are paused — could be intentional (decommissioned target)
/// or forgotten (paused for maintenance and never re-enabled).
/// Mapped by ISO 27001:2022 A.8.13 and NIS2 Art. 21(c).
/// </summary>
internal sealed class DisabledBackupJobsCheck : IComplianceCheck
{
    public string Id => "backup.job-disabled";
    public string Title => "Disabled backup jobs should be reviewed periodically";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.BackupJobs];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var jobs = ctx.Get<BackupJobInfo>(ComplianceDataKind.BackupJobs);

        foreach (var j in jobs)
        {
            if (j.Enabled) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Info,
                ScopeType = "backup-job",
                Scope = j.Id,
                Title = "Disabled backup job",
                Details = $"Backup job '{j.Id}' is currently disabled.",
                Remediation = "Re-enable if still needed, or delete if obsolete.",
            };
        }
    }
}
