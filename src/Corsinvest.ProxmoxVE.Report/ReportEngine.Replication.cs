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

        var sem = CreateSemaphore();
        var results = await Task.WhenAll(nodes.Select(async item =>
        {
            await sem.WaitAsync();
            try
            {
                ReportGlobal($"Replication: {item.Node}");
                return (item.Node,
                        rows: (await client.Nodes[item.Node].Replication.GetAsync())
                            .Select(a => new
                            {
                                item.Node,
                                a.Id,
                                a.Type,
                                a.VmType,
                                VmId = a.Guest,
                                GuestName = long.TryParse(a.Guest, out var guestId)
                                                && _resourcesByVmId.TryGetValue(guestId, out var guestRes)
                                            ? guestRes.Name ?? ""
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
                            }).ToList());
            }
            finally { sem.Release(); }
        }));

        var sw = CreateSheetWriter(workbook, "Replication");
        sw.CreateTable(null,
                       results.OrderBy(r => r.Node).SelectMany(r => r.rows).ToList(),
                       tbl => sw.ApplyReplicationLinks(tbl));
        sw.AdjustColumns();

        return results.Sum(r => r.rows.Count);
    }
}
