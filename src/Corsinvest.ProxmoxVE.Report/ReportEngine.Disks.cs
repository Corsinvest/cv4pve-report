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
    private void AppendDiskRows(XLWorkbook workbook, ClusterResource vm, IEnumerable<VmDisk> disks)
    {
        var rows = disks.Select(a =>
        {
            var storageRes = _resources.FirstOrDefault(r => r.ResourceType == ClusterResourceType.Storage
                                                            && r.Node == vm.Node
                                                            && r.Storage == a.Storage);
            return new
            {
                vm.Node,
                vm.VmId,
                VmName = vm.Name,
                VmType = vm.Type,
                VmStatus = vm.Status,
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
        })
        .OrderBy(a => a.Id)
        .ToList();

        _diskSw ??= CreateSheetWriter(workbook, "Disks");
        _diskSw.CreateOrAddTable(ref _diskTable,
                                 null,
                                 rows,
                                 tbl =>
                                 {
                                     _diskSw.ApplyNodeLinks(tbl);
                                     _diskSw.ApplyVmIdLinks(tbl);
                                     _diskSw.ApplyStorageLinks(tbl);
                                 });
    }
}
