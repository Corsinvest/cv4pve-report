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
    private async Task<int> AddClusterPoolsDataAsync()
    {
        if (!settings.Cluster.Include) { return 0; }

        using var sw = _writer.AddSection("Cluster Pools");

        ReportGlobal("Cluster Pools");
        var pools = await client.Pools.GetAsync().ToSafeEnum(_issues, "Cluster Pools", LinkKey.ClusterPools);

        var poolResults = await RunParallelAsync(pools,
                                                 async pool =>
                                                 {
                                                     var detail = await client.Pools[pool.Id].GetAsync()
                                                                              .ToSafeSingle(_issues, "Cluster Pools", LinkKey.ClusterPools);
                                                     return (pool, members: detail?.Members ?? []);
                                                 });
        if (_compliance.IsRequired(ComplianceDataKind.Pools))
        {
            _compliance.Provide(ComplianceDataKind.Pools,
                                poolResults.Select(r => new Compliance.Models.PoolInfo(
                                    Id: r.pool.Id,
                                    MemberCount: r.members.Count())).ToList());
        }

        var poolRows = poolResults.SelectMany(r => r.members.Select(member => new
        {
            Pool = r.pool.Id,
            member.Type,
            member.Node,
            member.VmId,
            member.Storage,
            member.Status,
            DescriptionWrap = member.Description,
            CommentWrap = r.pool.Comment,
        })).ToList();
        sw.AddTable("Pools", poolRows,
                    new TableOptions<dynamic>()
                        .WithNodeLink(r => (string?)r.Node)
                        .WithVmIdLink(r => r.VmId is long id ? id : null)
                        .WithStorageLink(r => (string?)r.Storage));

        return poolRows.Count;
    }
}
