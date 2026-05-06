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

        // Register storage→sheet links so other tables can hyperlink "Storage" cells back here.
        // This was previously embedded inside the table .Select; pulled out so registration
        // doesn't depend on enumeration order during table materialization.
        foreach (var a in filtered)
        {
            _writer.Links[SheetLinkKey(ClusterResourceType.Storage, StorageNode(a), a.Storage)] = "Storages";
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
                        DiskSizeGB = ToGB(a.DiskSize),
                        DiskUsageGB = ToGB(a.DiskUsage),
                        DiskUsagePct = a.DiskUsagePercentage,
                    }),
                    new TableOptions<dynamic>()
                        .WithNodeLink<dynamic>(r => (string?)r.Node)
                        .WithStorageLink<dynamic>(r => (string?)r.Storage));

        return Task.FromResult(filtered.Count);
    }
}
