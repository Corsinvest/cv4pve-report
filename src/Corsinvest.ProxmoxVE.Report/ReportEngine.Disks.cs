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
    private void AppendDiskRows(ClusterResource vm, IEnumerable<VmDisk> disks)
    {
        if (!settings.Guest.IncludeDisksSheet) { return; }
        _pendingDiskRows.Add((vm, disks));
    }

    private int WriteDiskData(XLWorkbook workbook)
    {
        if (_pendingDiskRows.Count == 0) { return 0; }

        var count = _pendingDiskRows.Sum(e => e.Disks.Count());
        var rows = _pendingDiskRows
            .SelectMany(entry => entry.Disks.Select(a =>
            {
                var storageRes = _resources.FirstOrDefault(r => r.ResourceType == ClusterResourceType.Storage
                                                                && r.Node == entry.Vm.Node
                                                                && r.Storage == a.Storage);
                return new
                {
                    entry.Vm.Node,
                    entry.Vm.VmId,
                    VmName = entry.Vm.Name,
                    VmType = entry.Vm.Type,
                    VmStatus = entry.Vm.Status,
                    a.Id,
                    a.Storage,
                    StorageType = storageRes?.PluginType,
                    StorageSharedFlag = ToX(storageRes?.Shared),
                    a.FileName,
                    SizeGB = ToGB(a.SizeBytes),
                    StorageUsagePct = storageRes is { DiskSize: > 0 }
                                        ? (double)a.SizeBytes / storageRes.DiskSize
                                        : (double?)null,
                    a.Cache,
                    BackupFlag = ToX(a.Backup),
                    IsUnusedFlag = ToX(a.IsUnused),
                    a.Device,
                    a.MountPoint,
                    a.MountSourcePath,
                    PassthroughFlag = ToX(a.Passthrough),
                    a.Prealloc,
                    a.Format,
                };
            }))
            .OrderBy(a => a.Node)
            .ThenBy(a => a.VmId)
            .ThenBy(a => a.Id)
            .ToList();

        var diskSw = CreateSheetWriter(workbook, "Disks");
        diskSw.CreateTable(null,
                            rows,
                            tbl =>
                            {
                                diskSw.ApplyNodeLinks(tbl);
                                diskSw.ApplyVmIdLinks(tbl);
                                diskSw.ApplyStorageLinks(tbl);
                            });
        diskSw.WriteIndex();
        diskSw.AdjustColumns();
        _pendingDiskRows.Clear();

        return count;
    }
}
