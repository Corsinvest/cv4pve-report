/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Resilience;

/// <summary>
/// Flags running VMs/CTs that are not part of any HA resource. Auditors expect
/// critical workloads to be HA-managed so they survive a node failure.
/// Severity is intentionally <see cref="Compliance.Severity.Low"/>: not every
/// workload needs HA (test VMs, ephemeral CTs) — the auditor decides per scope.
/// Mapped by ISO 27001:2022 A.8.14 and NIS2 Art. 21(c).
/// </summary>
internal sealed class VmsWithoutHaResourceCheck : IComplianceCheck
{
    public string Id => "resilience.vm-without-ha";
    public string Title => "Running guests should be managed by High Availability";

    public IReadOnlyList<ComplianceDataKind> Requires =>
    [
        ComplianceDataKind.HaResources,
        ComplianceDataKind.Vms,
    ];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var ha = ctx.Get<HaResourceInfo>(ComplianceDataKind.HaResources);
        var vms = ctx.Get<VmInfo>(ComplianceDataKind.Vms);

        var haVmIds = ha.Where(h => h.VmId.HasValue).Select(h => h.VmId!.Value).ToHashSet();

        foreach (var vm in vms)
        {
            if (vm.IsTemplate) { continue; }
            if (!vm.Status.Equals("running", StringComparison.OrdinalIgnoreCase)) { continue; }
            if (haVmIds.Contains(vm.VmId)) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Low,
                ScopeType = vm.Type,
                Scope = vm.VmId.ToString(),
                ScopeName = vm.Name,
                Title = "Guest not under HA",
                Details = $"{vm.Type} {vm.VmId} ({vm.Name}) on node {vm.Node} is running but is not managed by any HA resource.",
                Remediation = "If the workload is business-critical, add it to a HA group in Datacenter → HA.",
            };
        }
    }
}
