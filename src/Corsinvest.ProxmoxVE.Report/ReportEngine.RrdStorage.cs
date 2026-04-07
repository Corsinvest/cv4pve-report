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
    private async Task AddRrdStorageDataAsync(XLWorkbook workbook)
    {
        if (!settings.Storage.RrdData.Enabled) { return; }

        var all = GetResources(ClusterResourceType.Storage)
                            .OrderBy(a => a.Id)
                            .ToList();

        var filtered = all.GroupBy(a => a.Shared
                                            ? $"shared:{a.Storage}"
                                            : $"{a.Node}:{a.Storage}")
                          .Select(g => g.First())
                          .ToList();

        if (filtered.Count == 0) { return; }

        var rrdTimeFrame = settings.Storage.RrdData.TimeFrame.GetValue();
        var rrdConsolidation = settings.Storage.RrdData.Consolidation.GetValue();
        var semaphore = new SemaphoreSlim(settings.Storage.RrdData.MaxParallelRequests);

        var tasks = filtered.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                ReportGlobal($"RRD Storage: {item.Node} {item.Storage}");

                var rows = (await client.Nodes[item.Node].Storage[item.Storage].Rrddata.GetAsync(rrdTimeFrame, rrdConsolidation))
                            .Select(a => new
                            {
                                Node = item.Shared ? "(shared)" : item.Node,
                                item.Storage,
                                a.TimeDate,
                                SizeGB = ToGB(a.Size),
                                UsedGB = ToGB(a.Used),
                                UsagePct = a.Size > 0 ? (double)a.Used / a.Size : (double?)null,
                            }).ToList();

                return (item, rows);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        var sw = CreateSheetWriter(workbook, "RRD Storage");
        IXLTable? table = null;

        foreach (var (_, rows) in results.OrderBy(r => r.item.Id))
        {
            sw.CreateOrAddTable(ref table,
                                null,
                                rows,
                                tbl =>
                                {
                                    sw.ApplyNodeLinks(tbl);
                                    sw.ApplyStorageLinks(tbl);
                                });
        }

        sw.AdjustColumns();
    }
}
