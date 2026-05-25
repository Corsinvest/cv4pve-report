/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddClusterSdnDataAsync()
    {
        if (!settings.Cluster.Include) { return 0; }

        using var sw = _writer.AddSection("Cluster SDN");

        ReportGlobal("Cluster SDN: Fetching data");

        var sdnControllersTask = client.Cluster.Sdn.Controllers.GetAsync().ToSafeEnum(_issues, "Cluster SDN", LinkKey.ClusterSdn);
        var sdnIpamsTask = client.Cluster.Sdn.Ipams.GetAsync().ToSafeEnum(_issues, "Cluster SDN", LinkKey.ClusterSdn);

        await Task.WhenAll(sdnControllersTask, sdnIpamsTask);

        sw.AddTable("Zones",
                    _sdnZones.Select(a => new
                    {
                        a.Zone,
                        a.Type,
                        a.Mtu,
                        a.Nodes,
                        a.Bridge,
                        a.Controller,
                        a.Ipam,
                        a.Dns,
                        a.State,
                    }));

        sw.AddTable("Vnets",
                    _sdnVnets.Select(a => new
                    {
                        a.Vnet,
                        a.Zone,
                        a.Type,
                        a.Tag,
                        a.Alias,
                        a.VlanAware,
                        a.State
                    }));

        sw.AddTable("Controllers",
                    sdnControllersTask.Result.Select(a => new
                    {
                        a.Controller,
                        a.Type,
                        a.Asn,
                        a.Peers,
                        a.Node,
                        a.State
                    }));

        sw.AddTable("Ipams",
                    sdnIpamsTask.Result.Select(a => new
                    {
                        a.Ipam,
                        a.Type,
                    }));

        var subnetResults = await RunParallelAsync(_sdnVnets,
                                                   async vnet => (vnet,
                                                                  subs: await client.Cluster.Sdn.Vnets[vnet.Vnet].Subnets.GetAsync()
                                                                                    .ToSafeEnum(_issues, "Cluster SDN", LinkKey.ClusterSdn)));
        sw.AddTable("Subnets",
                    subnetResults.SelectMany(r => r.subs.Select(subnet => new
                    {
                        r.vnet.Vnet,
                        subnet.Subnet,
                        subnet.Type,
                        subnet.Gateway,
                        subnet.Snat,
                        subnet.DhcpDnsServer,
                        subnet.DnsZonePrefix,
                    })).ToList());

        return _sdnVnets.Count + _sdnZones.Count + sdnControllersTask.Result.Count;
    }
}
