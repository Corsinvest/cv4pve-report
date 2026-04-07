/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private void AppendPartitionRows(XLWorkbook workbook,
                                     ClusterResource vm,
                                     IEnumerable<VmQemuAgentGetFsInfo.ResultInt> partitions)
    {
        _partitionSw ??= CreateSheetWriter(workbook, "Partitions");
        _partitionSw.CreateOrAddTable(ref _partitionTable,
                                      null,
                                      partitions.Select(a => new
                                      {
                                          vm.Node,
                                          vm.VmId,
                                          VmName = vm.Name,
                                          vm.VmType,
                                          VmStatus = vm.Status,
                                          a.MountPoint,
                                          a.Type,
                                          TotalGB = ToGB(a.TotalBytes),
                                          UsedGB = ToGB(a.UsedBytes),

                                          UsedPct = a.TotalBytes > 0
                                                        ? (double)a.UsedBytes / a.TotalBytes
                                                        : (double?)null,

                                          ErrorWrap = a.Error?.ToString() ?? "",
                                          a.Name,
                                          DisksWrap = a.Disks.Select(d => $"{d.Dev} ({d.BusType}:{d.Bus}:{d.Target}:{d.Unit}) [{d.PciController?.Domain:X4}:{d.PciController?.Bus:X2}:{d.PciController?.Slot:X2}.{d.PciController?.Function}]")
                                                                .JoinAsString(Environment.NewLine),
                                      }).ToList(),
                                      tbl =>
                                      {
                                          _partitionSw.ApplyNodeLinks(tbl);
                                          _partitionSw.ApplyVmIdLinks(tbl);
                                      });

    }
}
