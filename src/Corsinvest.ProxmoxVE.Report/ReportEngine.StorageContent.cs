/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task AddStorageContentDataAsync(XLWorkbook workbook)
    {
        if (!settings.Storage.Content.IncludeContent && !settings.Storage.Content.IncludeBackups) { return; }

        var all = GetResources(ClusterResourceType.Storage)
                            .OrderBy(a => a.Id)
                            .ToList();

        var filtered = all.GroupBy(a => a.Shared ? $"shared:{a.Storage}" : $"{a.Node}:{a.Storage}")
                          .Select(a => a.First())
                          .Where(a => !a.IsUnknown)
                          .ToList();

        if (filtered.Count == 0) { return; }

        double? StorageUsagePct(long size, ulong storageSize)
            => storageSize > 0
                ? (double)size / storageSize
                : null;

        string FormatVmId(long vmId)
            => vmId > 0
                ? vmId.ToString()
                : "";

        string GuestName(long vmId)
            => vmId > 0
                ? _resources.FirstOrDefault(r => r.VmId == vmId)?.Name ?? ""
                : "";

        var semaphore = new SemaphoreSlim(settings.Storage.Content.MaxParallelRequests);

        var tasks = filtered.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                ReportGlobal($"Storage Content: {item.Node} {item.Storage}");

                var allContent = await client.Nodes[item.Node].Storage[item.Storage].Content.GetAsync();

                var storageSize = item.DiskSize;

                var contentRows = settings.Storage.Content.IncludeContent
                                    ? allContent.Where(a => !string.Equals(a.Content, "backup", StringComparison.OrdinalIgnoreCase))
                                               .Select(a => new
                                               {
                                                   Node = item.Shared 
                                                            ? "(shared)" 
                                                            : item.Node,

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

                var backupRows = settings.Storage.Content.IncludeBackups
                                    ? allContent.Where(a => string.Equals(a.Content, "backup", StringComparison.OrdinalIgnoreCase))
                                               .Select(a => new
                                               {
                                                   Node = item.Shared 
                                                            ? "(shared)" 
                                                            : item.Node,
                                                            
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

        SheetWriter? contentSw = null;
        IXLTable? contentTable = null;
        SheetWriter? backupSw = null;
        IXLTable? backupTable = null;

        foreach (var (_, contentRows, backupRows) in results.OrderBy(r => r.item.Id))
        {
            if (contentRows.Count > 0)
            {
                contentSw ??= CreateSheetWriter(workbook, "Storage Content");
                contentSw.CreateOrAddTable(ref contentTable,
                                           null,
                                           contentRows,
                                           tbl =>
                                           {
                                               contentSw.ApplyNodeLinks(tbl);
                                               contentSw.ApplyStorageLinks(tbl);
                                               contentSw.ApplyVmIdLinks(tbl);
                                           });
            }

            if (backupRows.Count > 0)
            {
                backupSw ??= CreateSheetWriter(workbook, "Backups");
                backupSw.CreateOrAddTable(ref backupTable,
                                          null,
                                          backupRows,
                                          tbl =>
                                          {
                                              backupSw.ApplyNodeLinks(tbl);
                                              backupSw.ApplyStorageLinks(tbl);
                                              backupSw.ApplyVmIdLinks(tbl);
                                          });
            }
        }

        contentSw?.AdjustColumns();
        backupSw?.AdjustColumns();
    }
}
