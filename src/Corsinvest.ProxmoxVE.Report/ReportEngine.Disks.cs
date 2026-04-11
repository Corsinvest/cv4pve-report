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

        var storageLookup = _resources.Where(a => a.ResourceType == ClusterResourceType.Storage)
                                      .ToDictionary(a => (a.Node, a.Storage));

        var count = _pendingDiskRows.Sum(e => e.Disks.Count());

        var sw = CreateSheetWriter(workbook, "Disks");
        sw.CreateTable(null,
                       _pendingDiskRows.SelectMany(entry => entry.Disks.Select(a =>
                       {
                           storageLookup.TryGetValue((entry.Vm.Node, a.Storage), out var storageRes);

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
                       .ToList(),
                       tbl =>
                       {
                           sw.ApplyNodeLinks(tbl);
                           sw.ApplyVmIdLinks(tbl);
                           sw.ApplyStorageLinks(tbl);
                       });

        sw.WriteIndex();
        sw.AdjustColumns();
        _pendingDiskRows.Clear();

        return count;
    }
}
