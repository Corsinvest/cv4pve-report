/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddSnapshotsDataAsync(XLWorkbook workbook)
    {
        if (!settings.Guest.IncludeSnapshotsSheet) { return 0; }

        var resources = GetResources(ClusterResourceType.Vm)
                                .Where(a => !a.IsUnknown)
                                .OrderBy(a => a.Id)
                                .ToList();

        if (resources.Count == 0) { return 0; }

        var sw = CreateSheetWriter(workbook, "Snapshots");
        var semaphore = CreateSemaphore();

        var tasks = resources.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                ReportGlobal($"Snapshots: {item.Node} {item.VmId}");

                var rows = new List<dynamic>();
                foreach (var snapshot in await SnapshotHelper.GetSnapshotsAsync(client, item.Node, item.VmType, item.VmId))
                {
                    rows.Add(new
                    {
                        item.Node,
                        item.VmId,
                        VmName = item.Name,
                        item.VmType,
                        Snapshot = snapshot.Name,
                        snapshot.Parent,
                        snapshot.Date,
                        IncludeRamFlag = ToX(snapshot.VmStatus),

                        SizeGB = SnapshotSizeProvider != null
                                    ? ToGB(await SnapshotSizeProvider(item.Node, item.VmType, item.VmId, snapshot.Name))
                                    : (double?)null,

                        DescriptionWrap = snapshot.Description,
                    });
                }
                return (item, rows);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        sw.CreateTable(null,
                       results.OrderBy(r => r.item.Id).SelectMany(r => r.rows).ToList(),
                       tbl =>
                       {
                           sw.ApplyNodeLinks(tbl);
                           sw.ApplyVmIdLinks(tbl);
                       });
        sw.AdjustColumns();

        return results.Sum(r => r.rows.Count);
    }
}
