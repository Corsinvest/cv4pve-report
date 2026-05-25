/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Report.Compliance;
using Corsinvest.ProxmoxVE.Report.Helpers;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddClusterTasksDataAsync()
    {
        if (!settings.Cluster.IncludeTasks) { return 0; }

        var tasks = await client.Cluster.Tasks.GetAsync()
                                .ToSafeEnum(_issues, "Cluster Tasks", LinkKey.ClusterTasks);

        if (_compliance.IsRequired(ComplianceDataKind.ClusterTasks))
        {
            _compliance.Provide(ComplianceDataKind.ClusterTasks,
                                tasks.Select(t => new Compliance.Models.ClusterTaskInfo(
                                    Type: t.Type ?? "",
                                    Node: t.Node ?? "",
                                    User: t.User,
                                    Status: t.Status,
                                    StatusOk: t.StatusOk,
                                    StartTimeUnix: t.StartTime,
                                    EndTimeUnix: t.EndTime)).ToList());
        }

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
