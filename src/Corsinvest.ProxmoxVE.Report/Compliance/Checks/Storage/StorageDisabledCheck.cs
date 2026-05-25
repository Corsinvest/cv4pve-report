/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Storage;

/// <summary>
/// Informational: storages configured but disabled. Either intentional
/// (maintenance) or forgotten — auditor should know which.
/// Mapped by ISO 27001:2022 A.8.13.
/// </summary>
internal sealed class StorageDisabledCheck : IComplianceCheck
{
    public string Id => "storage.disabled";
    public string Title => "Configured storages should not stay disabled";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Storages];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var storages = ctx.Get<StorageInfo>(ComplianceDataKind.Storages);

        // Each storage row repeats per node when not shared; dedup on (storage, shared scope).
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in storages)
        {
            if (s.Enabled) { continue; }
            var key = s.Shared
                        ? $"shared:{s.Storage}"
                        : $"{s.Node}:{s.Storage}";
            if (!seen.Add(key)) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Info,
                ScopeType = "storage",
                Scope = s.Storage,
                ScopeName = s.Shared ? "(shared)" : s.Node,
                Title = "Storage disabled",
                Details = $"Storage '{s.Storage}' (type {s.Type}) is configured but disabled.",
                Remediation = "Re-enable in Datacenter → Storage, or delete the entry if obsolete.",
            };
        }
    }
}
