/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private record VmDiskRow(long VmId,
                             string Name,
                             string Node,
                             string Type,
                             string Status,
                             VmDisk Disk);

    private async Task AddDisksDataAsync(XLWorkbook workbook)
    {
        var sw = CreateSheetWriter(workbook, "Disks");

        sw.ReserveIndexRows(3);

        await WriteClusterStorageAsync(sw, "Storage Configuration");

        WriteStorage(sw);

        sw.CreateTable("VM Disks",
                       _vmDiskRows.Select(a => new
                       {
                           a.Node,
                           a.VmId,
                           a.Name,
                           a.Type,
                           a.Status,
                           a.Disk.Id,
                           a.Disk.Storage,
                           a.Disk.FileName,
                           SizeGB = ToGB(a.Disk.SizeBytes),
                           a.Disk.Cache,
                           a.Disk.Backup,
                           a.Disk.IsUnused,
                           a.Disk.Device,
                           a.Disk.MountPoint,
                           a.Disk.MountSourcePath,
                           a.Disk.Passthrough,
                           a.Disk.Prealloc,
                           a.Disk.Format,
                       }),
                       tbl =>
                       {
                           sw.ApplyNodeLinks(tbl);
                           sw.ApplyVmIdLinks(tbl);
                           sw.ApplyStorageLinks(tbl);
                       });

        sw.WriteIndex();
        sw.AdjustColumns();
    }
}
