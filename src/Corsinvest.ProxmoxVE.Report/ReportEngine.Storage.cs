/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task AddStoragesDataAsync(XLWorkbook workbook)
    {
        var sw = CreateSheetWriter(workbook, "Storages");

        var all = GetResources(ClusterResourceType.Storage)
                            .OrderBy(a => a.Id)
                            .ToList();

        // Deduplicate: shared storage appears once per cluster, non-shared once per node
        var filtered = all.GroupBy(a => a.Shared ? $"shared:{a.Storage}" : $"{a.Node}:{a.Storage}")
                          .Select(g => g.First())
                          .ToList();

        var pt = new ProgressTracker(_progress, filtered.Count);

        sw.CreateTable(null,
                       filtered.Select(a =>
                       {
                           var node = StorageNode(a);

                           _sheetLinks[SheetLinkKey(ClusterResourceType.Storage, node, a.Storage)] = "Storages";

                           return new
                           {
                               Node = node,
                               a.Storage,
                               a.Status,
                               a.PluginType,
                               Content = ToNewLine(a.Content),
                               SharedFlag = ToX(a.Shared),
                               DiskSizeGB = ToGB(a.DiskSize),
                               DiskUsageGB = ToGB(a.DiskUsage),
                               DiskUsagePct = a.DiskUsagePercentage,
                           };
                       }),
                      tbl =>
                      {
                          sw.ApplyNodeLinks(tbl);
                          sw.ApplyStorageLinks(tbl);
                      });

        sw.AdjustColumns();
    }
}
