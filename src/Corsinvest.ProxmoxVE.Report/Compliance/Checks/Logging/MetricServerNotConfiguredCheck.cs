/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Logging;

/// <summary>
/// Flags clusters that have NO enabled external metric server (InfluxDB / Graphite).
/// Without external metrics, long-term monitoring relies only on the in-node RRD
/// which is short-lived and lost on reboot — auditors require persistent
/// observability evidence for incident investigation.
/// Mapped by ISO 27001:2022 A.8.16 and NIS2 Art. 21(b).
/// </summary>
internal sealed class MetricServerNotConfiguredCheck : IComplianceCheck
{
    public string Id => "logging.no-external-metric-server";
    public string Title => "Cluster should export metrics to an external time-series database";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.MetricServers];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var servers = ctx.Get<MetricServerInfo>(ComplianceDataKind.MetricServers);

        if (servers.Any(s => !s.Disabled)) { yield break; }

        yield return new ComplianceFinding
        {
            CheckId = Id,
            Severity = Severity.Medium,
            ScopeType = "cluster",
            Scope = "cluster",
            Title = "No enabled external metric server configured",
            Details = servers.Count == 0
                        ? "The cluster has no metric server configured at all."
                        : "All configured metric servers are disabled.",
            Remediation = "Configure an InfluxDB or Graphite metric server in Datacenter → Metric Server and enable it.",
        };
    }
}
