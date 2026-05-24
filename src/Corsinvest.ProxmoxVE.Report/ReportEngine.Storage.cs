/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private Task<int> AddStoragesDataAsync()
    {
        var filtered = _uniqueStorages.OrderBy(a => a.Id).ToList();

        foreach (var a in filtered)
        {
            _writer.Links[LinkKey.Storage(StorageNode(a), a.Storage)] = "Storages";
        }

        using var sw = _writer.AddSection("Storages");
        sw.AddTable(null,
                    filtered.ConvertAll(a => new
                    {
                        Node = StorageNode(a),
                        a.Storage,
                        a.Status,
                        a.PluginType,
                        Content = ToNewLine(a.Content),
                        SharedFlag = ToX(a.Shared),
                        DiskSizeGB = a.DiskSize,
                        DiskUsageGB = a.DiskUsage,
                        DiskUsagePct = a.DiskUsagePercentage,
                    }),
                    new TableOptions<dynamic>()
                        .WithNodeLink(r => (string?)r.Node)
                        .WithStorageLink(r => (string?)r.Storage));

        return Task.FromResult(filtered.Count);
    }
}
