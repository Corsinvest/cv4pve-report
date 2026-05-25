/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Logging;

/// <summary>
/// Informational: cluster log entries at error severity (syslog pri 0-3).
/// Flags clusters with more than the warning threshold so the auditor knows
/// the log volume that requires triage.
/// Mapped by ISO 27001:2022 A.8.15 and NIS2 Art. 21(b).
/// </summary>
internal sealed class ClusterLogErrorsCheck : IComplianceCheck
{
    private const int Threshold = 10;

    public string Id => "logging.cluster-log-errors";
    public string Title => "Cluster log should not contain unaddressed error entries";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.ClusterLog];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var entries = ctx.Get<ClusterLogEntryInfo>(ComplianceDataKind.ClusterLog);

        // syslog priorities: 0 emerg .. 3 err. Anything >= 4 is warn/info/debug.
        var errors = entries.Count(e => e.Pri >= 0 && e.Pri <= 3);
        if (errors < Threshold) { yield break; }

        yield return new ComplianceFinding
        {
            CheckId = Id,
            Severity = Severity.Info,
            ScopeType = "cluster",
            Scope = "cluster",
            Title = "Error-level entries in cluster log",
            Details = $"{errors} log entries at error severity or above were observed in the captured window.",
            Remediation = "Review Cluster → Log; treat recurring errors as incident triage candidates.",
        };
    }
}
