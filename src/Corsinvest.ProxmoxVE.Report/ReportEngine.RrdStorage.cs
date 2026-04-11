/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddRrdStorageDataAsync(XLWorkbook workbook)
    {
        if (!settings.Storage.RrdData.Enabled) { return 0; }

        var filtered = _uniqueStorages.OrderBy(a => a.Id).ToList();
        if (filtered.Count == 0) { return 0; }

        var rrdTimeFrame = settings.Storage.RrdData.TimeFrame.GetValue();
        var rrdConsolidation = settings.Storage.RrdData.Consolidation.GetValue();
        var semaphore = CreateSemaphore();

        var tasks = filtered.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                ReportGlobal($"RRD Storage: {item.Node} {item.Storage}");

                return (item,
                        rows: (await client.Nodes[item.Node].Storage[item.Storage].Rrddata.GetAsync(rrdTimeFrame, rrdConsolidation))
                            .Select(a => new
                            {
                                Node = StorageNode(item),
                                item.Storage,
                                a.TimeDate,
                                SizeGB = ToGB(a.Size),
                                UsedGB = ToGB(a.Used),
                                UsagePct = a.Size > 0 ? (double)a.Used / a.Size : (double?)null,
                            }).ToList());
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        var sw = CreateSheetWriter(workbook, "RRD Storage");
        sw.CreateTable(null,
                       results.OrderBy(r => r.item.Id).SelectMany(r => r.rows).ToList(),
                       tbl =>
                       {
                           sw.ApplyNodeLinks(tbl);
                           sw.ApplyStorageLinks(tbl);
                       });
        sw.AdjustColumns();

        return results.Sum(r => r.rows.Count);
    }
}
