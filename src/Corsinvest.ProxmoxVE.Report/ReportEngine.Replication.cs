/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddReplicationDataAsync()
    {
        if (!settings.Node.IncludeReplication) { return 0; }

        var nodes = GetResources(ClusterResourceType.Node)
                              .Where(a => !a.IsUnknown)
                              .OrderBy(a => a.Id)
                              .ToList();

        if (nodes.Count == 0) { return 0; }

        var results = await RunParallelAsync(nodes, async item =>
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
        });

        var rows = results.OrderBy(r => r.Node).SelectMany(r => r.rows).ToList();

        using var sw = _writer.AddSection("Replication");
        sw.AddTable(null, rows,
                    new TableOptions<dynamic>().WithReplicationLinks(
                        nodeSelector: r => (string?)r.Node,
                        vmIdSelector: r => long.TryParse((string?)r.VmId, out var id) ? id : (long?)null,
                        sourceSelector: r => (string?)r.Source,
                        targetSelector: r => (string?)r.Target));

        return rows.Count;
    }
}
