/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private void AppendNodeNetworkRows(XLWorkbook workbook, string node, IEnumerable<NodeNetwork> nets)
    {
        var rows = nets.Select(a => new
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
        })
        .ToList();

        _networkSw ??= CreateSheetWriter(workbook, "Network");
        if (_networkNodeTable == null)
        {
            _networkSw.ReserveIndexRows(2);
            _networkNodeTable = _networkSw.CreateTable("Nodes Networks",
                                                       rows,
                                                       tbl => _networkSw.ApplyNodeLinks(tbl));
        }
        else
        {
            _networkSw.AppendData(_networkNodeTable,
                                  rows,
                                  tbl => _networkSw.ApplyNodeLinks(tbl));
        }
    }

    private void AppendVmNetworkRows(XLWorkbook workbook, VmNetworkRow row)
    {
        _networkSw ??= CreateSheetWriter(workbook, "Network");
        _networkSw.CreateOrAddTable(ref _networkVmTable,
                                    "VM Networks",
                                    [(new
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
                                    })],
                                    tbl =>
                                    {
                                        _networkSw.ApplyNodeLinks(tbl);
                                        _networkSw.ApplyVmIdLinks(tbl);
                                    });
    }
}
