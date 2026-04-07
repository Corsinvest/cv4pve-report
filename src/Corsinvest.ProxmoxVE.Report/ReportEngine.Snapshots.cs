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
    private async Task AddSnapshotsDataAsync(XLWorkbook workbook)
    {
        if (!settings.Guest.Snapshots.Enabled) { return; }

        var resources = GetResources(ClusterResourceType.Vm)
                                .Where(a => !a.IsUnknown)
                                .OrderBy(a => a.Id)
                                .ToList();

        if (resources.Count == 0) { return; }

        var sw = CreateSheetWriter(workbook, "Snapshots");
        IXLTable? table = null;
        var semaphore = new SemaphoreSlim(settings.Guest.Snapshots.MaxParallelRequests);

        var tasks = resources.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                ReportGlobal($"Snapshots: {item.Node} {item.VmId}");

                var rawSnapshots = await SnapshotHelper.GetSnapshotsAsync(client,
                                                                          item.Node,
                                                                          item.VmType,
                                                                          item.VmId);
                var rows = new List<dynamic>();
                foreach (var a in rawSnapshots)
                {
                    long? sizeBytes = SnapshotSizeProvider != null
                                        ? await SnapshotSizeProvider(item.Node, item.VmType, item.VmId, a.Name)
                                        : null;

                    rows.Add(new
                    {
                        item.Node,
                        item.VmId,
                        VmName = item.Name,
                        item.VmType,
                        Snapshot = a.Name,
                        a.Parent,
                        a.Date,
                        IncludeRamFlag = ToX(a.VmStatus),

                        SizeGB = sizeBytes.HasValue
                                    ? ToGB(sizeBytes.Value)
                                    : (double?)null,

                        DescriptionWrap = a.Description,
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

        foreach (var (_, rows) in results.OrderBy(r => r.item.Id))
        {
            sw.CreateOrAddTable(ref table,
                                null,
                                rows,
                                tbl =>
                                {
                                    sw.ApplyNodeLinks(tbl);
                                    sw.ApplyVmIdLinks(tbl);
                                });
        }

        sw.AdjustColumns();
    }
}
