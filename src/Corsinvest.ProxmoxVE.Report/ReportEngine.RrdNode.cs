/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddRrdNodeDataAsync()
    {
        if (!settings.Node.RrdData.Enabled) { return 0; }

        var nodes = GetResources(ClusterResourceType.Node)
                              .Where(a => !a.IsUnknown)
                              .OrderBy(a => a.Id)
                              .ToList();

        if (nodes.Count == 0) { return 0; }

        var results = await RunParallelAsync(nodes, async item =>
        {
            ReportGlobal($"RRD Nodes: {item.Node}");
            var data = await client.Nodes[item.Node].Rrddata
                                   .GetAsync(settings.Node.RrdData.TimeFrame, settings.Node.RrdData.Consolidation)
                                   .ToSafeEnum(_issues, "RRD Nodes", LinkKey.Node(item.Node));
            return (item,
                    rows: data.Select(a => new
                    {
                        item.Node,
                        a.TimeDate,
                        CpuUsagePct = a.CpuUsagePercentage,
                        IoWaitPct = a.IoWait,
                        a.Loadavg,
                        MemorySizeGB = a.MemorySize,
                        MemoryUsageGB = a.MemoryUsage,
                        MemoryUsagePct = a.MemoryUsagePercentage,
                        SwapSizeGB = a.SwapSize,
                        SwapUsageGB = a.SwapUsage,
                        RootSizeGB = a.RootSize,
                        RootUsageGB = a.RootUsage,
                        NetInMB = a.NetIn,
                        NetOutMB = a.NetOut,
                        PsiCpuSomePct = a.PressureCpuSome,
                        PsiIoSomePct = a.PressureIoSome,
                        PsiIoFullPct = a.PressureIoFull,
                        PsiMemSomePct = a.PressureMemorySome,
                        PsiMemFullPct = a.PressureMemoryFull,
                    }).ToList());
        });

        var rows = results.OrderBy(r => r.item.Id).SelectMany(r => r.rows).ToList();

        using var sw = _writer.AddSection("RRD Nodes");
        sw.AddTable(null,
                    rows,
                    new TableOptions<dynamic>().WithNodeLink(r => (string?)r.Node));

        return results.Sum(r => r.rows.Count);
    }
}
