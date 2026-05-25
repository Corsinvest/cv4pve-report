/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Backup;

/// <summary>
/// Flags running VMs/CTs not covered by any enabled backup job. Templates and
/// stopped guests are excluded.
/// Mapped by ISO 27001:2022 A.8.13 and NIS2 Art. 21(c).
/// </summary>
internal sealed class VmsWithoutBackupJobCheck : IComplianceCheck
{
    public string Id => "backup.vm-without-backup-job";
    public string Title => "Running VMs and containers must be covered by a backup job";

    public IReadOnlyList<ComplianceDataKind> Requires =>
    [
        ComplianceDataKind.BackupJobs,
        ComplianceDataKind.Vms,
    ];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var jobs = ctx.Get<BackupJobInfo>(ComplianceDataKind.BackupJobs).Where(j => j.Enabled).ToList();
        var vms = ctx.Get<VmInfo>(ComplianceDataKind.Vms);

        if (jobs.Any(j => j.All)) { yield break; }

        var includedIds = jobs.SelectMany(j => j.VmIds).ToHashSet();

        foreach (var vm in vms)
        {
            if (vm.IsTemplate) { continue; }
            if (!vm.Status.Equals("running", StringComparison.OrdinalIgnoreCase)) { continue; }
            if (includedIds.Contains(vm.VmId)) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.High,
                ScopeType = vm.Type,
                Scope = vm.VmId.ToString(),
                ScopeName = vm.Name,
                Title = "Guest without backup coverage",
                Details = $"{vm.Type} {vm.VmId} ({vm.Name}) on node {vm.Node} is running but is not included in any enabled backup job.",
                Remediation = "Add the guest to an existing job or create a new backup job in Datacenter → Backup.",
            };
        }
    }
}
