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
    private async Task<int> AddClusterLogDataAsync()
    {
        if (!settings.Cluster.Log.Enabled) { return 0; }

        var logs = await client.Cluster.Log.GetAsync(max: settings.Cluster.Log.MaxCount > 0
                                                            ? settings.Cluster.Log.MaxCount
                                                            : null)
                               .ToSafeEnum(_issues, "Cluster Log", LinkKey.ClusterLog);

        if (_compliance.IsRequired(ComplianceDataKind.ClusterLog))
        {
            _compliance.Provide(ComplianceDataKind.ClusterLog,
                                logs.Select(l => new Compliance.Models.ClusterLogEntryInfo(
                                    TimeUnix: l.Time,
                                    Node: l.Node,
                                    User: l.User,
                                    Pri: l.Severity,
                                    Tag: l.Service,
                                    Message: l.Message)).ToList());
        }

        using var sw = _writer.AddSection("Cluster Log");
        sw.AddTable(null,
                    logs.Select(a => new
                    {
                        a.TimeDate,
                        a.Node,
                        a.User,
                        a.SeverityEnum,
                        a.Service,
                        a.Message,
                    }),
                    new TableOptions<dynamic>().WithNodeLink(r => (string?)r.Node));

        return logs.Count;
    }
}
