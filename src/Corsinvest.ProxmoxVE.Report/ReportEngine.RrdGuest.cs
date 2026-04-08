/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task AddRrdGuestDataAsync(XLWorkbook workbook)
    {
        if (!settings.Guest.RrdData.Enabled) { return; }

        var vmIds = _vmIds;
        var guests = GetResources(ClusterResourceType.Vm)
                               .OrderBy(a => a.Id)
                               .ToList();

        if (guests.Count == 0) { return; }

        var rrdTimeFrame = settings.Guest.RrdData.TimeFrame.GetValue();
        var rrdConsolidation = settings.Guest.RrdData.Consolidation.GetValue();
        var semaphore = new SemaphoreSlim(settings.MaxParallelRequests);

        var tasks = guests.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                ReportGlobal($"RRD Guest: {item.VmId} {item.Name}");

                var rrdData = item.VmType == VmType.Qemu
                    ? await client.Nodes[item.Node].Qemu[item.VmId].Rrddata.GetAsync(rrdTimeFrame, rrdConsolidation)
                    : await client.Nodes[item.Node].Lxc[item.VmId].Rrddata.GetAsync(rrdTimeFrame, rrdConsolidation);

                var rows = rrdData.Select(a => new
                {
                    item.Node,
                    Type = item.VmType.ToString(),
                    item.VmId,
                    item.Name,
                    a.TimeDate,
                    CpuUsagePct = a.CpuUsagePercentage,
                    MemorySizeGB = ToGB(a.MemorySize),
                    MemoryUsageGB = ToGB(a.MemoryUsage),
                    MemoryUsagePct = a.MemoryUsagePercentage,
                    NetInMB = ToMB(a.NetIn),
                    NetOutMB = ToMB(a.NetOut),
                    DiskReadMB = ToMB(a.DiskRead),
                    DiskWriteMB = ToMB(a.DiskWrite),
                    DiskSizeGB = ToGB(a.DiskSize),
                    DiskUsageGB = ToGB(a.DiskUsage),
                    DiskUsagePct = a.DiskUsagePercentage,
                    PsiCpuSomePct = a.PressureCpuSome,
                    PsiCpuFullPct = a.PressureCpuFull,
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

        var sw = CreateSheetWriter(workbook, "RRD Guests");
        IXLTable? table = null;

        foreach (var (_, rows) in results.OrderBy(r => r.item.Id))
        {
            sw.CreateOrAddTable(ref table,
                                null,
                                rows,
                                tbl =>
                                {
                                    sw.ApplyVmIdLinks(tbl);
                                    sw.ApplyNodeLinks(tbl);
                                });
        }

        sw.AdjustColumns();
    }
}
