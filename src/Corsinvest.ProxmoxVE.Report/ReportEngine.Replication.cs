/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddReplicationDataAsync(XLWorkbook workbook)
    {
        if (!settings.Node.IncludeReplicationSheet) { return 0; }

        var nodes = GetResources(ClusterResourceType.Node)
                              .Where(a => !a.IsUnknown)
                              .OrderBy(a => a.Id)
                              .ToList();

        if (nodes.Count == 0) { return 0; }

        SheetWriter? sw = null;
        IXLTable? table = null;

        foreach (var item in nodes)
        {
            ReportGlobal($"Replication: {item.Node}");

            var rows = (await client.Nodes[item.Node].Replication.GetAsync())
                        .Select(a => new
                        {
                            item.Node,
                            a.Id,
                            a.Type,
                            a.VmType,
                            VmId = a.Guest,

                            GuestName = long.TryParse(a.Guest, out var guestId)
                                            ? _resources.FirstOrDefault(r => r.VmId == guestId)?.Name ?? ""
                                            : "",

                            a.Source,
                            a.Target,
                            a.Schedule,
                            a.Disable,
                            a.FailCount,
                            a.Error,
                            a.Duration,
                            a.Rate,
                            LastSync = FromUnixTime(a.LastSync),
                            NextSync = FromUnixTime(a.NextSync),
                            LastTry = FromUnixTime(a.LastTry),
                            a.JobNum,
                            CommentWrap = a.Comment,
                        }).ToList();

            sw ??= CreateSheetWriter(workbook, "Replication");
            sw.CreateOrAddTable(ref table,
                                null,
                                rows,
                                tbl => sw.ApplyReplicationLinks(tbl));
        }

        sw?.AdjustColumns();

        return nodes.Count;
    }
}
