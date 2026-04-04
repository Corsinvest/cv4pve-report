/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private Task AddNetworkDataAsync(XLWorkbook workbook)
    {
        var sw = CreateSheetWriter(workbook, "Network");

        var tableCount = 2; // Node Networks + VM Networks
        sw.ReserveIndexRows(tableCount);

        sw.CreateTable("Nodes Networks",
                       _nodeNetworks.SelectMany(kv => kv.Value.Select(a => new
                       {
                           Node = kv.Key,
                           a.Active,
                           a.AutoStart,
                           a.Exists,
                           a.Type,
                           a.Interface,
                           a.LinkType,
                           a.Method,
                           a.Cidr,
                           a.Address,
                           a.Netmask,
                           a.Gateway,
                           a.Method6,
                           a.Cidr6,
                           a.Address6,
                           a.Netmask6,
                           a.Gateway6,
                           a.Priority,
                           a.Mtu,
                           a.BondMode,
                           a.BondMiimon,
                           a.BondPrimary,
                           a.BondXmitHashPolicy,
                           a.Slaves,
                           a.BridgeStp,
                           a.BridgeVlanAware,
                           a.BridgeVids,
                           a.BridgeFd,
                           a.BridgePorts,
                           a.VlanId,
                           a.VlanRawDevice,
                           a.VlanProtocol,
                           a.OvsBridge,
                           a.OvsBonds,
                           a.OvsPorts,
                           a.OvsOptions,
                           a.OvsTag,
                           a.VxlanId,
                           a.VxlanLocalTunnelIp,
                           a.VxlanPhysDev,
                           a.Comments,
                           a.Comments6,
                       })),
                       tbl => sw.ApplyNodeLinks(tbl));

        sw.CreateTable("VM Networks",
                       _vmNetworkRows.Select(a => new
                       {
                           a.VmId,
                           a.Name,
                           a.Node,
                           a.Type,
                           a.Status,
                           a.Hostname,
                           a.OsInfo,
                           a.IsInternal,
                           a.Network.MacAddress,
                           a.Network.Bridge,
                           a.Network.Tag,
                           a.Network.Model,
                           a.Network.Firewall,
                           a.Network.IpAddress,
                           a.Network.IpAddress6,
                           a.Network.Gateway,
                           a.Network.Gateway6,
                           a.Network.Mtu,
                           a.Network.Rate,
                       }),
                       tbl =>
                       {
                           sw.ApplyNodeLinks(tbl);
                           sw.ApplyVmIdLinks(tbl);
                       });

        sw.WriteIndex();
        sw.AdjustColumns();

        return Task.CompletedTask;
    }
}
