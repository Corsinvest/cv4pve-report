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
    private void AppendPartitionRows(ClusterResource vm,
                                     IEnumerable<VmQemuAgentGetFsInfo.ResultInfo> partitions)
    {
        if (!settings.Guest.IncludePartitionsSheet) { return; }
        _pendingPartitionRows.Add((vm, partitions));
    }

    private int WritePartitionData(XLWorkbook workbook)
    {
        if (_pendingPartitionRows.Count == 0) { return 0; }

        var count = _pendingPartitionRows.Sum(e => e.Partitions.Count());

        var sw = CreateSheetWriter(workbook, "Partitions");
        sw.CreateTable(null,
                        _pendingPartitionRows.SelectMany(entry => entry.Partitions.Select(a => new
                        {
                            entry.Vm.Node,
                            entry.Vm.VmId,
                            VmName = entry.Vm.Name,
                            entry.Vm.VmType,
                            VmStatus = entry.Vm.Status,
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
                        })).ToList(),
                        tbl =>
                        {
                            sw.ApplyNodeLinks(tbl);
                            sw.ApplyVmIdLinks(tbl);
                        });

        sw.AdjustColumns();
        _pendingPartitionRows.Clear();

        return count;
    }
}
