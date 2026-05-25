/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Report.Helpers;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddRrdStorageDataAsync()
    {
        if (!settings.Storage.RrdData.Enabled) { return 0; }

        var filtered = _uniqueStorages.OrderBy(a => a.Id).ToList();
        if (filtered.Count == 0) { return 0; }

        var results = await RunParallelAsync(filtered, async item =>
        {
            ReportGlobal($"RRD Storage: {item.Node} {item.Storage}");
            var data = await client.Nodes[item.Node].Storage[item.Storage].Rrddata
                                   .GetAsync(settings.Storage.RrdData.TimeFrame, settings.Storage.RrdData.Consolidation)
                                   .ToSafeEnum(_issues, "RRD Storage", LinkKey.Node(item.Node));
            return (item,
                    rows: data.Select(a => new
                    {
                        Node = StorageNode(item),
                        item.Storage,
                        a.TimeDate,
                        SizeGB = a.Size,
                        UsedGB = a.Used,
                        UsagePct = a.Size > 0 ? (double)a.Used / a.Size : (double?)null,
                    }).ToList());
        });

        var rows = results.OrderBy(r => r.item.Id).SelectMany(r => r.rows).ToList();

        using var sw = _writer.AddSection("RRD Storage");
        sw.AddTable(null,
                    rows,
                    new TableOptions<dynamic>()
                        .WithNodeLink(r => (string?)r.Node)
                        .WithStorageLink(r => (string?)r.Storage));

        return results.Sum(r => r.rows.Count);
    }
}
