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
    private async Task<int> AddRrdGuestDataAsync()
    {
        if (!settings.Guest.RrdData.Enabled) { return 0; }

        var guests = GetResources(ClusterResourceType.Vm)
                               .OrderBy(a => a.Id)
                               .ToList();

        if (guests.Count == 0) { return 0; }

        var results = await RunParallelAsync(guests, async item =>
        {
            ReportGlobal($"RRD Guest: {item.VmId} {item.Name}");
            var data = await client.GetVmRrdDataAsync(item.Node,
                                                     item.VmType,
                                                     item.VmId,
                                                     settings.Guest.RrdData.TimeFrame,
                                                     settings.Guest.RrdData.Consolidation)
                                   .ToSafeEnum(_issues, "RRD Guests", LinkKey.Vm(item.VmId));
            return (item,
                    rows: data.Select(a => new
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
        });

        var rows = results.OrderBy(r => r.item.Id).SelectMany(r => r.rows).ToList();

        using var sw = _writer.AddSection("RRD Guests");
        sw.AddTable(null,
                    rows,
                    new TableOptions<dynamic>()
                        .WithVmIdLink(r => r.VmId is long id ? id : (long?)null)
                        .WithNodeLink(r => (string?)r.Node));

        return results.Sum(r => r.rows.Count);
    }
}
