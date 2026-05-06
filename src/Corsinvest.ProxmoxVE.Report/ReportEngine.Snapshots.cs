/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddSnapshotsDataAsync()
    {
        if (!settings.Guest.IncludeSnapshotsSheet) { return 0; }

        var resources = GetResources(ClusterResourceType.Vm)
                                .Where(a => !a.IsUnknown)
                                .OrderBy(a => a.Id)
                                .ToList();

        if (resources.Count == 0) { return 0; }

        var results = await RunParallelAsync(resources, async item =>
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
        });

        var rows = results.OrderBy(r => r.item.Id).SelectMany(r => r.rows).ToList();

        using var sw = _writer.AddSection("Snapshots");
        sw.AddTable(null, rows,
                    new TableOptions<dynamic>()
                        .WithNodeLink<dynamic>(r => (string?)r.Node)
                        .WithVmIdLink<dynamic>(r => r.VmId is long id ? id : (long?)null));

        return results.Sum(r => r.rows.Count);
    }
}
