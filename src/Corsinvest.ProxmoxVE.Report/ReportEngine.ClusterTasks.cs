/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Extension;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddClusterTasksDataAsync(XLWorkbook workbook)
    {
        if (!settings.Cluster.IncludeTasksSheet) { return 0; }

        var tasks = (await client.Cluster.Tasks.GetAsync()).ToList();

        var sw = CreateSheetWriter(workbook, "Cluster Tasks");
        sw.CreateTable(null,
                       tasks.Select(a => new
                       {
                           a.Node,
                           a.UniqueTaskId,
                           a.Type,
                           a.User,
                           a.Status,
                           StatusOkFlag = ToX(a.StatusOk),
                           StartTime = a.StartTimeDate,
                           EndTime = a.EndTimeDate,
                           a.Duration,
                       }),
                       tbl => sw.ApplyNodeLinks(tbl));

        sw.AdjustColumns();

        return tasks.Count;
    }
}
