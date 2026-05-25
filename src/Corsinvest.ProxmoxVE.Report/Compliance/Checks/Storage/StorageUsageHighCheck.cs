/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Storage;

/// <summary>
/// Flags storages over 85% usage. Runaway capacity is a documented backup /
/// availability risk: snapshots fail and writes stall.
/// Mapped by ISO 27001:2022 A.8.6 and NIS2 Art. 21(c).
/// </summary>
internal sealed class StorageUsageHighCheck : IComplianceCheck
{
    private const double WarnPct = 0.85;
    private const double CritPct = 0.95;

    public string Id => "storage.usage-high";
    public string Title => "Storage usage should stay below 85%";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Storages];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var storages = ctx.Get<StorageInfo>(ComplianceDataKind.Storages);

        foreach (var s in storages)
        {
            if (!s.Enabled) { continue; }
            if (s.UsagePct is not double usage) { continue; }
            if (usage < WarnPct) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = usage >= CritPct ? Severity.Critical : Severity.High,
                ScopeType = "storage",
                Scope = s.Storage,
                ScopeName = s.Shared ? "(shared)" : s.Node,
                Title = "Storage usage above threshold",
                Details = $"Storage '{s.Storage}' on node {s.Node} is at {usage * 100:F1}% usage.",
                Remediation = "Free space, expand the storage, or migrate guests off this storage.",
            };
        }
    }
}
