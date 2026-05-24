/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private void AppendPartitionRows(ClusterResource vm,
                                     IEnumerable<VmQemuAgentGetFsInfo.ResultInfo> partitions)
    {
        if (!settings.Guest.IncludePartitions) { return; }
        _pendingPartitionRows.Add((vm, partitions));
    }

    private int WritePartitionData()
    {
        if (_pendingPartitionRows.Count == 0) { return 0; }

        var count = _pendingPartitionRows.Sum(e => e.Partitions.Count());

        using var sw = _writer.AddSection("Partitions");
        sw.AddTable(null,
                    _pendingPartitionRows.SelectMany(entry => entry.Partitions.Select(a => new
                    {
                        entry.Vm.Node,
                        entry.Vm.VmId,
                        VmName = entry.Vm.Name,
                        entry.Vm.VmType,
                        VmStatus = entry.Vm.Status,
                        a.MountPoint,
                        a.Type,
                        TotalGB = a.TotalBytes,
                        UsedGB = a.UsedBytes,

                        UsedPct = a.TotalBytes > 0
                                    ? (double)a.UsedBytes / a.TotalBytes
                                    : (double?)null,

                        ErrorWrap = a.Error?.ToString() ?? "",
                        a.Name,
                        DisksWrap = a.Disks.Select(d => $"{d.Dev} ({d.BusType}:{d.Bus}:{d.Target}:{d.Unit}) [{d.PciController?.Domain:X4}:{d.PciController?.Bus:X2}:{d.PciController?.Slot:X2}.{d.PciController?.Function}]")
                                           .JoinAsString(Environment.NewLine),
                    })).ToList(),
                    new TableOptions<dynamic>()
                        .WithNodeLink(r => (string?)r.Node)
                        .WithVmIdLink(r => r.VmId is long id ? id : null));

        _pendingPartitionRows.Clear();

        return count;
    }
}
