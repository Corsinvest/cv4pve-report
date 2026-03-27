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
    private async Task AddStoragesDataAsync(XLWorkbook workbook)
    {
        var sw = new SheetWriter(workbook.Worksheets.Add("Storages"), _sheetLinks);
        var resources = await client.GetResourcesAsync(ClusterResourceType.Storage);
        var filtered = resources.Where(a => CheckNames(settings.Storage.Names, a.Storage)).OrderBy(a => a.Node).ToList();
        var items = new List<dynamic>();
        var pt = new ProgressTracker(_progress, filtered.Count);

        foreach (var item in filtered)
        {
            pt.Next(item);

            items.Add(new
            {
                item.Node,
                item.Storage,
                item.Status,
                item.PluginType,
                Content = item.Content.Replace(",", Environment.NewLine),
                item.Shared,
                DiskSizeGB = ToGB(item.DiskSize),
                DiskUsageGB = ToGB(item.DiskUsage),
                item.DiskUsagePercentage
            });

            if (!item.IsUnknown) { await AddStorageDetailAsync(workbook, item, pt); }
        }

        sw.CreateTable("Storages", items, tbl =>
        {
            sw.ApplyNodeLinks(tbl);
            sw.ApplyStorageLinks(tbl);
        });
        sw.AdjustColumns();
    }

    private async Task AddStorageDetailAsync(XLWorkbook workbook, ClusterResource item, ProgressTracker pt)
    {
        var node = item.Node;
        var storage = item.Storage;
        var sw = new SheetWriter(workbook.Worksheets.Add(GetSheetName(ClusterResourceType.Storage, node, storage)!), _sheetLinks);

        sw.WriteKeyValue($"{node} - {storage}",
                         new()
                         {
                             ["Node"] = item.Node,
                             ["Storage"] = item.Storage,
                             ["Status"] = item.Status,
                             ["Type"] = item.PluginType,
                             ["Shared"] = item.Shared ? "Yes" : "No",
                             ["Size"] = $"{ToGB(item.DiskSize):0.##} GB",
                             ["Used"] = $"{ToGB(item.DiskUsage):0.##} GB",
                             ["Used %"] = $"{item.DiskUsagePercentage:P1}",
                         });

        var tableCount = 1  // Content
                       + (settings.Storage.RrdData.Enabled ? 1 : 0);

        sw.ReserveIndexRows(tableCount);

        pt.Step("Content");
        sw.CreateTable("Content",
                       (await client.Nodes[node].Storage[storage].Content.GetAsync())
                       .Select(a => new
                       {
                           a.Content,
                           a.ContentDescription,
                           a.CreationDate,
                           a.Encrypted,
                           a.FileName,
                           a.Format,
                           a.Name,
                           a.Notes,
                           a.Protected,
                           SizeGB = ToGB(a.Size),
                           a.Verified,
                           VmId = a.VmId > 0 ? a.VmId.ToString() : ""
                       }),
                       tbl => sw.ApplyVmIdLinks(tbl));

        if (settings.Storage.RrdData.Enabled)
        {
            pt.Step("RRD");
            sw.CreateTable("RRD Data",
                           (await client.Nodes[node].Storage[storage].Rrddata.GetAsync(settings.Storage.RrdData.TimeFrame.GetValue(),
                                                                                       settings.Storage.RrdData.Consolidation.GetValue()))
                               .Select(a => new
                               {
                                   a.TimeDate,
                                   UsedGB = ToGB(a.Used),
                                   SizeGB = ToGB(a.Size)
                               }));
        }

        sw.WriteIndex();
        sw.AdjustColumns();
    }
}
