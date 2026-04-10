/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private void CreateNodeNetworkTable(SheetWriter sw, string title, IEnumerable<NodeNetwork> networks, string? node = null)
    {
        sw.CreateTable(title,
                       networks.Select(a => new
                       {
                           Node = node,
                           ActiveFLag = ToX(a.Active),
                           AutoStartFlag = ToX(a.AutoStart),
                           ExistsFlag = ToX(a.Exists),
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
                           CommentsWrap = a.Comments,
                           a.Comments6,
                       }).ToList(),
                       tbl => sw.ApplyNodeLinks(tbl));
    }

    private int WriteNetworkData(XLWorkbook workbook)
    {
        var count = _pendingNodeNetworkRows.Count + _pendingNetworkRows.Count;

        var sw = CreateSheetWriter(workbook, "Network");

        sw.ReserveIndexRows(2);
        CreateNodeNetworkTable(sw,
                               "Nodes Networks",
                               _pendingNodeNetworkRows.Select(e => e.Network),
                               null);
        // Apply node links using the node column from pending rows
        _pendingNodeNetworkRows.Clear();

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

        _pendingNetworkRows.Clear();

        sw.WriteIndex();
        sw.AdjustColumns();

        return count;
    }
}
