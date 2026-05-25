/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Backup;

/// <summary>
/// Flags failed vzdump tasks within the cluster task log window. A backup that
/// was scheduled but never succeeded leaves the cluster unprotected.
/// Mapped by ISO 27001:2022 A.8.13 and NIS2 Art. 21(c).
/// </summary>
internal sealed class RecentBackupFailureCheck : IComplianceCheck
{
    public string Id => "backup.recent-failure";
    public string Title => "Backup tasks should complete successfully";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.ClusterTasks];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var tasks = ctx.Get<ClusterTaskInfo>(ComplianceDataKind.ClusterTasks);

        var failed = tasks.Where(t => t.Type.StartsWith("vzdump", StringComparison.OrdinalIgnoreCase) && !t.StatusOk)
                          .ToList();

        foreach (var t in failed)
        {
            var when = t.EndTimeUnix > 0
                        ? DateTimeOffset.FromUnixTimeSeconds(t.EndTimeUnix).UtcDateTime
                        : DateTime.UtcNow;

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.High,
                ScopeType = "node",
                Scope = t.Node,
                Title = "Failed backup task",
                Details = $"vzdump task on node {t.Node} ended on {when:yyyy-MM-dd HH:mm} with status '{t.Status}'.",
                Remediation = "Inspect the task log (Node → Tasks) and resolve the underlying issue.",
            };
        }
    }
}
