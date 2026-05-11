/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddStorageContentDataAsync()
    {
        if (!settings.Storage.IncludeContent && !settings.Storage.IncludeBackups) { return 0; }

        var filtered = _uniqueStorages.Where(a => !a.IsUnknown).OrderBy(a => a.Id).ToList();
        if (filtered.Count == 0) { return 0; }

        static double? StorageUsagePct(long size, ulong storageSize)
            => storageSize > 0
                ? (double)size / storageSize
                : null;

        static string FormatVmId(long vmId)
            => vmId > 0
                ? vmId.ToString()
                : "";

        string GuestName(long vmId)
            => vmId > 0 && _resourcesByVmId.TryGetValue(vmId, out var res)
                ? res.Name ?? ""
                : "";

        var results = await RunParallelAsync(filtered, async item =>
        {
            ReportGlobal($"Storage Content: {item.Node} {item.Storage}");

            var allContent = await client.Nodes[item.Node].Storage[item.Storage].Content
                                         .GetAsync()
                                         .ToSafeEnum(_issues, "Storage Content", LinkKey.Node(item.Node));
            var storageSize = item.DiskSize;

            var contentRows = settings.Storage.IncludeContent
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

            var backupRows = settings.Storage.IncludeBackups
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
        });
        var ordered = results.OrderBy(r => r.item.Id).ToList();

        // VmId in these tables is a string (FormatVmId converts it). Build the link by
        // column name explicitly rather than via the long-typed WithVmIdLink shorthand.
        static TableOptions<dynamic> ContentLinks() => new TableOptions<dynamic>()
            .WithNodeLink(r => (string?)r.Node)
            .WithStorageLink(r => (string?)r.Storage)
            .WithColumnLink("VmId", r => long.TryParse((string?)r.VmId, out var id) && id > 0 ? LinkKey.Vm(id) : null);

        if (settings.Storage.IncludeContent)
        {
            using var sw = _writer.AddSection("Storage Content");
            sw.AddTable(null, ordered.SelectMany(r => r.contentRows).ToList(), ContentLinks());
        }

        if (settings.Storage.IncludeBackups)
        {
            using var sw = _writer.AddSection("Backups");
            sw.AddTable(null, ordered.SelectMany(r => r.backupRows).ToList(), ContentLinks());
        }

        return results.Sum(r => r.contentRows.Count + r.backupRows.Count);
    }
}
