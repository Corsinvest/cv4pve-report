/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Logging;

/// <summary>
/// Informational: cluster task error rate. Many failed tasks in the report
/// window indicate instability that auditors expect to be tracked.
/// Mapped by ISO 27001:2022 A.8.15 and NIS2 Art. 21(b).
/// </summary>
internal sealed class HighTaskErrorRateCheck : IComplianceCheck
{
    private const double WarnRatio = 0.10;

    public string Id => "logging.high-task-error-rate";
    public string Title => "Cluster task error rate should stay low";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.ClusterTasks];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var tasks = ctx.Get<ClusterTaskInfo>(ComplianceDataKind.ClusterTasks);
        if (tasks.Count == 0) { yield break; }

        var failed = tasks.Count(t => !t.StatusOk);
        var ratio = (double)failed / tasks.Count;
        if (ratio < WarnRatio) { yield break; }

        yield return new ComplianceFinding
        {
            CheckId = Id,
            Severity = Severity.Low,
            ScopeType = "cluster",
            Scope = "cluster",
            Title = "High cluster task error rate",
            Details = $"{failed}/{tasks.Count} ({ratio * 100:F1}%) of recent cluster tasks failed.",
            Remediation = "Inspect Cluster → Tasks for recurring failures and address the root cause.",
        };
    }
}
