/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Report.Compliance;
using Corsinvest.ProxmoxVE.Report.Helpers;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddClusterHaDataAsync()
    {
        if (!settings.Cluster.Include) { return 0; }

        using var sw = _writer.AddSection("Cluster HA");

        ReportGlobal("Cluster HA: Fetching data");

        var pveVersion = await client.Version.GetAsync();
        var pveMajor = int.TryParse(pveVersion.Version?.Split('.')[0], out var v)
                        ? v
                        : 0;
        var haGroupsSupported = pveMajor < 9;

        var haResourcesTask = client.Cluster.Ha.Resources.GetAsync().ToSafeEnum(_issues, "Cluster HA", LinkKey.ClusterHa);
        var haStatusTask = client.Cluster.Ha.Status.Current.GetAsync().ToSafeEnum(_issues, "Cluster HA", LinkKey.ClusterHa);
        var haGroupsTask = haGroupsSupported
                            ? client.Cluster.Ha.Groups.GetAsync().ToSafeEnum(_issues, "Cluster HA", LinkKey.ClusterHa)
                            : Task.FromResult<IReadOnlyList<ClusterHaGroup>>([]);

        await Task.WhenAll(haResourcesTask, haStatusTask, haGroupsTask);

        if (_compliance.IsRequired(ComplianceDataKind.HaResources))
        {
            _compliance.Provide(ComplianceDataKind.HaResources,
                                haResourcesTask.Result.Select(a => new Compliance.Models.HaResourceInfo(
                                    Sid: a.Sid,
                                    Type: a.Type,
                                    Group: a.Group,
                                    State: a.State,
                                    VmId: ParseSidVmId(a.Sid))).ToList());
        }

        sw.AddTable("Resources",
                    haResourcesTask.Result.Select(a => new
                    {
                        a.Sid,
                        a.Type,
                        a.State,
                        a.Group,
                        a.Failback,
                        a.MaxRestart,
                        a.MaxRelocate,
                        a.Comment
                    }));

        if (haGroupsSupported)
        {
            sw.AddTable("Groups",
                        haGroupsTask.Result.Select(a => new
                        {
                            a.Group,
                            a.Nodes,
                            a.Nofailback,
                            a.Restricted,
                            a.Comment
                        }));
        }

        sw.AddTable("Status",
                    haStatusTask.Result.Select(a => new
                    {
                        a.Id,
                        a.Type,
                        a.Status,
                        a.Node,
                        a.Sid,
                        a.State,
                        a.CrmState,
                        a.RequestState,
                        QuorateFlag = ToX(a.Quorate),
                        FailbackFlag = ToX(a.Failback),
                        a.MaxRelocate,
                        a.MaxRestart,
                        Timestamp = FromUnixTime(a.Timestamp),
                    }));

        return haResourcesTask.Result.Count + haStatusTask.Result.Count;
    }

    private static long? ParseSidVmId(string? sid)
    {
        if (string.IsNullOrEmpty(sid)) { return null; }
        var idx = sid.IndexOf(':');
        if (idx < 0 || idx == sid.Length - 1) { return null; }
        return long.TryParse(sid.AsSpan(idx + 1), out var id) ? id : null;
    }
}
