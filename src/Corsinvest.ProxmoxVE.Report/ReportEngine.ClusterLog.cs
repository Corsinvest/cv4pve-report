/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Extension;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddClusterLogDataAsync(XLWorkbook workbook)
    {
        if (!settings.Cluster.Log.Enabled) { return 0; }

        var logs = (await client.Cluster.Log.GetAsync(max: settings.Cluster.Log.MaxCount > 0
                                                            ? settings.Cluster.Log.MaxCount
                                                            : null)).ToList();

        var sw = CreateSheetWriter(workbook, "Cluster Log");
        sw.CreateTable(null,
                       logs.Select(a => new
                       {
                           a.TimeDate,
                           a.Node,
                           a.User,
                           a.SeverityEnum,
                           a.Service,
                           a.Message,
                       }),
                       tbl => sw.ApplyNodeLinks(tbl));

        sw.AdjustColumns();

        return logs.Count;
    }
}
