/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Resilience;

/// <summary>
/// Flags running guests under HA without a corresponding replication job. HA
/// recovery on non-shared storage (ZFS) needs replication to be useful — without
/// it the VM can only restart on the node where its disks live.
/// Mapped by ISO 27001:2022 A.8.14 and NIS2 Art. 21(c).
/// </summary>
internal sealed class VmsWithoutReplicationCheck : IComplianceCheck
{
    public string Id => "resilience.ha-vm-without-replication";
    public string Title => "HA-managed guests should have a replication job";

    public IReadOnlyList<ComplianceDataKind> Requires =>
    [
        ComplianceDataKind.HaResources,
        ComplianceDataKind.ReplicationJobs,
        ComplianceDataKind.Vms,
    ];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var ha = ctx.Get<HaResourceInfo>(ComplianceDataKind.HaResources);
        var repl = ctx.Get<ReplicationJobInfo>(ComplianceDataKind.ReplicationJobs);
        var vms = ctx.Get<VmInfo>(ComplianceDataKind.Vms);

        var replicatedVmIds = repl.Where(r => !r.Disabled && r.GuestVmId.HasValue)
                                  .Select(r => r.GuestVmId!.Value)
                                  .ToHashSet();

        var vmById = vms.ToDictionary(v => v.VmId);

        foreach (var h in ha)
        {
            if (h.VmId is not long vmId) { continue; }
            if (replicatedVmIds.Contains(vmId)) { continue; }
            if (!vmById.TryGetValue(vmId, out var vm)) { continue; }
            if (!vm.Status.Equals("running", StringComparison.OrdinalIgnoreCase)) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Medium,
                ScopeType = vm.Type,
                Scope = vm.VmId.ToString(),
                ScopeName = vm.Name,
                Title = "HA guest without replication",
                Details = $"{vm.Type} {vm.VmId} ({vm.Name}) is in HA but has no enabled replication job — on shared-nothing storage HA failover requires replication.",
                Remediation = "Add a replication job in Datacenter → Replication, or confirm the guest's disks live on shared storage.",
            };
        }
    }
}
