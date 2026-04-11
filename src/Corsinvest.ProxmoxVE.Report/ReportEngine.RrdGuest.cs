/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddRrdGuestDataAsync(XLWorkbook workbook)
    {
        if (!settings.Guest.RrdData.Enabled) { return 0; }

        var guests = GetResources(ClusterResourceType.Vm)
                               .OrderBy(a => a.Id)
                               .ToList();

        if (guests.Count == 0) { return 0; }

        var semaphore = CreateSemaphore();

        var tasks = guests.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                ReportGlobal($"RRD Guest: {item.VmId} {item.Name}");

                return (item,
                        rows: (await client.GetVmRrdDataAsync(item.Node,
                                                             item.VmType,
                                                             item.VmId,
                                                             settings.Guest.RrdData.TimeFrame,
                                                             settings.Guest.RrdData.Consolidation))
                                .Select(a => new
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
                                }).ToList());
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        var sw = CreateSheetWriter(workbook, "RRD Guests");
        sw.CreateTable(null,
                       results.OrderBy(r => r.item.Id).SelectMany(r => r.rows).ToList(),
                       tbl =>
                       {
                           sw.ApplyVmIdLinks(tbl);
                           sw.ApplyNodeLinks(tbl);
                       });
        sw.AdjustColumns();

        return results.Sum(r => r.rows.Count);
    }
}
