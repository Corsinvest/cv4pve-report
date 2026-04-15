/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private void CreateNodeNetworkTable(SheetWriter sw, string title, IEnumerable<(string Node, NodeNetwork Network)> networks)
    {
        sw.CreateTable(title,
                       networks.Select(a => new
                       {
                           a.Node,
                           ActiveFlag = ToX(a.Network.Active),
                           AutoStartFlag = ToX(a.Network.AutoStart),
                           ExistsFlag = ToX(a.Network.Exists),
                           a.Network.Type,
                           a.Network.Interface,
                           a.Network.LinkType,
                           a.Network.Method,
                           a.Network.Cidr,
                           a.Network.Address,
                           a.Network.Netmask,
                           a.Network.Gateway,
                           a.Network.Method6,
                           a.Network.Cidr6,
                           a.Network.Address6,
                           a.Network.Netmask6,
                           a.Network.Gateway6,
                           a.Network.Priority,
                           a.Network.Mtu,
                           a.Network.BondMode,
                           a.Network.BondMiimon,
                           a.Network.BondPrimary,
                           a.Network.BondXmitHashPolicy,
                           a.Network.Slaves,
                           a.Network.BridgeStp,
                           a.Network.BridgeVlanAware,
                           a.Network.BridgeVids,
                           a.Network.BridgeFd,
                           a.Network.BridgePorts,
                           a.Network.VlanId,
                           a.Network.VlanRawDevice,
                           a.Network.VlanProtocol,
                           a.Network.OvsBridge,
                           a.Network.OvsBonds,
                           a.Network.OvsPorts,
                           a.Network.OvsOptions,
                           a.Network.OvsTag,
                           a.Network.VxlanId,
                           a.Network.VxlanLocalTunnelIp,
                           a.Network.VxlanPhysDev,
                           CommentsWrap = a.Network.Comments,
                           a.Network.Comments6,
                       }).ToList(),
                       tbl => sw.ApplyNodeLinks(tbl));
    }

    private int WriteNetworkData(XLWorkbook workbook)
    {
        var count = _pendingNodeNetworkRows.Count + _pendingNetworkRows.Count;

        var sw = CreateSheetWriter(workbook, "Network");

        sw.ReserveIndexRows(2);
        CreateNodeNetworkTable(sw, "Nodes Networks", _pendingNodeNetworkRows);

        sw.CreateTable("VM Networks",
                       _pendingNetworkRows.ConvertAll(row => new
                       {
                           row.Node,
                           row.VmId,
                           row.Name,
                           row.Type,
                           row.Status,
                           row.Hostname,
                           IsInternalFlag = ToX(row.IsInternal),
                           NetId = row.Network.Id,
                           NetName = row.Network.Name,
                           row.Network.MacAddress,
                           row.Network.Bridge,
                           row.Network.Tag,
                           row.Network.Model,
                           FirewallFlag = ToX(row.Network.Firewall),
                           row.Network.IpAddress,
                           row.Network.IpAddress6,
                           row.Network.Gateway,
                           row.Network.Gateway6,
                           row.Network.Mtu,
                           row.Network.Rate,
                       }),
                       tbl =>
                       {
                           sw.ApplyNodeLinks(tbl);
                           sw.ApplyVmIdLinks(tbl);
                       });

        sw.WriteIndex();
        sw.AdjustColumns();

        return count;
    }
}
