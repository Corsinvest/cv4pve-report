/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Logging;

/// <summary>
/// Informational: metric servers exist but every one is disabled. Different from
/// <see cref="MetricServerNotConfiguredCheck"/>: there configuration is missing,
/// here it is present but inert — usually a "paused for maintenance and forgotten"
/// situation.
/// Mapped by ISO 27001:2022 A.8.16 and NIS2 Art. 21(b).
/// </summary>
internal sealed class MetricServersAllDisabledCheck : IComplianceCheck
{
    public string Id => "logging.metric-servers-all-disabled";
    public string Title => "Configured metric servers should not all be disabled";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.MetricServers];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var servers = ctx.Get<MetricServerInfo>(ComplianceDataKind.MetricServers);

        if (servers.Count == 0) { yield break; }
        if (servers.Any(s => !s.Disabled)) { yield break; }

        var ids = string.Join(", ", servers.Select(s => s.Id));

        yield return new ComplianceFinding
        {
            CheckId = Id,
            Severity = Severity.Low,
            ScopeType = "cluster",
            Scope = "cluster",
            Title = "All metric servers disabled",
            Details = $"Cluster has metric servers configured ({ids}) but every one is disabled — metrics are not being shipped.",
            Remediation = "Re-enable a metric server in Datacenter → Metric Server, or remove the unused ones.",
        };
    }
}
