/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;

namespace Corsinvest.ProxmoxVE.Report.Helpers;

/// <summary>
/// Per-resource health score 0–100 (higher = healthier). Same formula as
/// cv4pve-admin so a Proxmox VE administrator sees the same number across tools.
///   Node           = 100 − (CPU·0.4 + RAM·0.4 + Disk·0.2)
///   VM (running)   = 100 − (CPU·0.5 + RAM·0.5)
///   VM (stopped)   = null (not measurable)
///   Storage        = 100 − Disk%
/// </summary>
internal static class HealthScore
{
    public static double? For(ClusterResource resource)
        => resource.ResourceType switch
        {
            ClusterResourceType.Node => Node(resource),
            ClusterResourceType.Vm => Vm(resource),
            ClusterResourceType.Storage => Storage(resource),
            _ => null,
        };

    private static double Node(ClusterResource r)
    {
        var cpu = r.CpuUsagePercentage * 100;
        var ram = r.MemoryUsagePercentage * 100;
        var disk = r.DiskSize > 0 ? (double)r.DiskUsage / r.DiskSize * 100 : 0;
        return Clamp(100 - ((cpu * 0.4) + (ram * 0.4) + (disk * 0.2)));
    }

    private static double? Vm(ClusterResource r)
    {
        if (r.Status != PveConstants.StatusVmRunning) { return null; }
        var cpu = r.CpuUsagePercentage * 100;
        var ram = r.MemoryUsagePercentage * 100;
        return Clamp(100 - ((cpu * 0.5) + (ram * 0.5)));
    }

    private static double Storage(ClusterResource r)
    {
        var disk = r.DiskSize > 0 ? (double)r.DiskUsage / r.DiskSize * 100 : 0;
        return Clamp(100 - disk);
    }

    private static double Clamp(double score) => Math.Round(Math.Clamp(score, 0, 100), 1);
}
