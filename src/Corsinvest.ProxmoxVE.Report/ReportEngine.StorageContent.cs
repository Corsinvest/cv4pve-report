/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Extension;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddStorageContentDataAsync(XLWorkbook workbook)
    {
        if (!settings.Storage.IncludeContentSheet && !settings.Storage.IncludeBackupsSheet) { return 0; }

        var filtered = _uniqueStorages.Where(a => !a.IsUnknown).OrderBy(a => a.Id).ToList();
        if (filtered.Count == 0) { return 0; }

        double? StorageUsagePct(long size, ulong storageSize)
            => storageSize > 0
                ? (double)size / storageSize
                : null;

        string FormatVmId(long vmId)
            => vmId > 0
                ? vmId.ToString()
                : "";

        string GuestName(long vmId)
            => vmId > 0 && _resourcesByVmId.TryGetValue(vmId, out var res)
                ? res.Name ?? ""
                : "";

        var semaphore = CreateSemaphore();

        var tasks = filtered.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                ReportGlobal($"Storage Content: {item.Node} {item.Storage}");

                var allContent = await client.Nodes[item.Node].Storage[item.Storage].Content.GetAsync();

                var storageSize = item.DiskSize;

                var contentRows = settings.Storage.IncludeContentSheet
                                    ? allContent.Where(a => !string.Equals(a.Content, "backup", StringComparison.OrdinalIgnoreCase))
                                               .Select(a => new
                                               {
                                                   Node = StorageNode(item),
                                                   item.Storage,
                                                   a.Content,
                                                   a.FileName,
                                                   a.Format,
                                                   SizeGB = ToGB(a.Size),
                                                   StorageUsagePct = StorageUsagePct(a.Size, storageSize),
                                                   VmId = FormatVmId(a.VmId),
                                                   GuestName = GuestName(a.VmId),
                                                   a.CreationDate,
                                                   NotesWrap = a.Notes,
                                                   a.Parent,
                                               })
                                               .ToList()
                                    : [];

                var backupRows = settings.Storage.IncludeBackupsSheet
                                    ? allContent.Where(a => string.Equals(a.Content, "backup", StringComparison.OrdinalIgnoreCase))
                                               .Select(a => new
                                               {
                                                   Node = StorageNode(item),
                                                   item.Storage,
                                                   a.FileName,
                                                   a.Format,
                                                   SizeGB = ToGB(a.Size),
                                                   StorageUsagePct = StorageUsagePct(a.Size, storageSize),
                                                   VmId = FormatVmId(a.VmId),
                                                   GuestName = GuestName(a.VmId),
                                                   a.CreationDate,
                                                   NotesWrap = a.Notes,
                                                   ProtectedFlag = ToX(a.Protected),
                                                   EncryptedFlag = ToX(a.Encrypted),
                                                   VerifiedFlag = ToX(a.Verified),
                                                   a.Parent,
                                               })
                                               .ToList()
                                    : [];

                return (item, contentRows, backupRows);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        var ordered = results.OrderBy(r => r.item.Id).ToList();

        if (settings.Storage.IncludeContentSheet)
        {
            var sw = CreateSheetWriter(workbook, "Storage Content");
            sw.CreateTable(null,
                           ordered.SelectMany(r => r.contentRows).ToList(),
                           tbl =>
                           {
                               sw.ApplyNodeLinks(tbl);
                               sw.ApplyStorageLinks(tbl);
                               sw.ApplyVmIdLinks(tbl);
                           });
            sw.AdjustColumns();
        }

        if (settings.Storage.IncludeBackupsSheet)
        {
            var sw = CreateSheetWriter(workbook, "Backups");
            sw.CreateTable(null,
                           ordered.SelectMany(r => r.backupRows).ToList(),
                           tbl =>
                           {
                               sw.ApplyNodeLinks(tbl);
                               sw.ApplyStorageLinks(tbl);
                               sw.ApplyVmIdLinks(tbl);
                           });
            sw.AdjustColumns();
        }

        return results.Sum(r => r.contentRows.Count + r.backupRows.Count);
    }
}
