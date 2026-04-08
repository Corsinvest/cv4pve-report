/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task AddRrdNodeDataAsync(XLWorkbook workbook)
    {
        if (!settings.Node.RrdData.Enabled) { return; }

        var nodes = GetResources(ClusterResourceType.Node)
                              .Where(a => !a.IsUnknown)
                              .OrderBy(a => a.Id)
                              .ToList();

        if (nodes.Count == 0) { return; }

        var rrdTimeFrame = settings.Node.RrdData.TimeFrame.GetValue();
        var rrdConsolidation = settings.Node.RrdData.Consolidation.GetValue();
        var semaphore = new SemaphoreSlim(settings.MaxParallelRequests);

        var tasks = nodes.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                ReportGlobal($"RRD Nodes: {item.Node}");

                var rows = (await client.Nodes[item.Node].Rrddata.GetAsync(rrdTimeFrame, rrdConsolidation))
                            .Select(a => new
                            {
                                item.Node,
                                a.TimeDate,
                                CpuUsagePct = a.CpuUsagePercentage,
                                a.IoWait,
                                a.Loadavg,
                                MemorySizeGB = ToGB(a.MemorySize),
                                MemoryUsageGB = ToGB(a.MemoryUsage),
                                MemoryUsagePct = a.MemoryUsagePercentage,
                                SwapSizeGB = ToGB(a.SwapSize),
                                SwapUsageGB = ToGB(a.SwapUsage),
                                RootSizeGB = ToGB(a.RootSize),
                                RootUsageGB = ToGB(a.RootUsage),
                                NetInMB = ToMB(a.NetIn),
                                NetOutMB = ToMB(a.NetOut),
                                PsiCpuSomePct = a.PressureCpuSome,
                                PsiIoSomePct = a.PressureIoSome,
                                PsiIoFullPct = a.PressureIoFull,
                                PsiMemSomePct = a.PressureMemorySome,
                                PsiMemFullPct = a.PressureMemoryFull,
                            }).ToList();

                return (item, rows);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        var sw = CreateSheetWriter(workbook, "RRD Nodes");
        IXLTable? table = null;

        foreach (var (_, rows) in results.OrderBy(r => r.item.Id))
        {
            sw.CreateOrAddTable(ref table, null, rows, tbl => sw.ApplyNodeLinks(tbl));
        }

        sw.AdjustColumns();
    }
}
