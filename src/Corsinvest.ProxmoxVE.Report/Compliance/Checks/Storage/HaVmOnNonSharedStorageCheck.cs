/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Storage;

/// <summary>
/// Heuristic: cluster has guests under HA but most storages are non-shared.
/// HA failover with non-shared storage requires replication; flag the
/// overall configuration when there's no shared content-capable storage.
/// Mapped by ISO 27001:2022 A.8.14 and NIS2 Art. 21(c).
/// </summary>
internal sealed class HaWithoutSharedStorageCheck : IComplianceCheck
{
    public string Id => "storage.ha-without-shared-storage";
    public string Title => "HA-enabled clusters should have at least one shared storage";

    public IReadOnlyList<ComplianceDataKind> Requires =>
    [
        ComplianceDataKind.Storages,
        ComplianceDataKind.HaResources,
    ];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var storages = ctx.Get<StorageInfo>(ComplianceDataKind.Storages);
        var ha = ctx.Get<HaResourceInfo>(ComplianceDataKind.HaResources);

        if (ha.Count == 0) { yield break; }
        if (storages.Any(s => s.Enabled && s.Shared)) { yield break; }

        yield return new ComplianceFinding
        {
            CheckId = Id,
            Severity = Severity.Low,
            ScopeType = "cluster",
            Scope = "cluster",
            Title = "HA configured but no shared storage",
            Details = $"{ha.Count} HA resource(s) configured but no enabled shared storage was found — HA relies on replication.",
            Remediation = "Add a shared storage (NFS, Ceph, iSCSI) so HA guests can fail over without replication windows.",
        };
    }
}
