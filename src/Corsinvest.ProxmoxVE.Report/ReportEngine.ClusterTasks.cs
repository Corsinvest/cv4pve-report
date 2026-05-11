/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddClusterTasksDataAsync()
    {
        if (!settings.Cluster.IncludeTasks) { return 0; }

        var tasks = await client.Cluster.Tasks.GetAsync()
                                .ToSafeEnum(_issues, "Cluster Tasks", LinkKey.ClusterTasks);

        using var sw = _writer.AddSection("Cluster Tasks");
        sw.AddTable(null,
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
                    new TableOptions<dynamic>().WithNodeLink(r => (string?)r.Node));

        return tasks.Count;
    }
}
