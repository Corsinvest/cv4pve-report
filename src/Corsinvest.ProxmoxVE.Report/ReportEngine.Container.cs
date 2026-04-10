/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private class CtFetchData
    {
        public required ClusterResource Item { get; init; }
        public VmConfigLxc? Config { get; init; }
        public List<VmNetworkRow> Networks { get; init; } = [];
    }

    private async Task<CtFetchData> FetchCtDataAsync(ClusterResource item, ProgressTracker pt)
    {
        pt.Next(item);

        pt.Step("Config");
        var config = item.IsUnknown
                        ? null
                        : await client.Nodes[item.Node].Lxc[item.VmId].Config.GetAsync();

        var networks = new List<VmNetworkRow>();
        if (config != null)
        {
            foreach (var net in config.Networks)
            {
                networks.Add(new(item.VmId,
                                 item.Name,
                                 item.Node,
                                 item.Type,
                                 item.Status,
                                 config.Hostname ?? "",
                                 net,
                                 IsInternal: false));
            }
        }

        return new()
        {
            Item = item,
            Config = config,
            Networks = networks
        };
    }

    private async Task<int> AddContainersDataAsync(XLWorkbook workbook)
    {
        var sw = CreateSheetWriter(workbook, "Containers");

        var resources = GetResources(ClusterResourceType.Vm)
                                  .Where(a => a.VmType == VmType.Lxc)
                                  .OrderBy(a => a.Id)
                                  .ToList();

        var items = new List<dynamic>();
        var pt = new ProgressTracker(_progress, resources.Count);

        var semaphore = CreateSemaphore();
        var tasks = resources.Select(async item =>
        {
            await semaphore.WaitAsync();
            try { return await FetchCtDataAsync(item, pt); }
            finally { semaphore.Release(); }
        });
        var results = (await Task.WhenAll(tasks)).OrderBy(d => d.Item.Id).ToList();

        foreach (var d in results)
        {
            var item = d.Item;
            var networks = d.Networks.Where(a => !a.IsInternal);

            _pendingNetworkRows.AddRange(d.Networks);

            items.Add(new
            {
                item.Node,
                item.VmId,
                item.Name,
                item.Status,
                item.Pool,
                Tags = ToNewLine(item.Tags, ";"),
                item.HaState,
                item.Lock,
                IsTemplateFlag = ToX(item.IsTemplate),
                item.CpuSize,
                CpuUsagePct = item.CpuUsagePercentage,
                HostCpuUsagePct = item.HostCpuUsage,
                MemorySizeGB = ToGB(item.MemorySize),
                MemoryUsageGB = ToGB(item.MemoryUsage),
                MemoryUsagePct = item.MemoryUsagePercentage,
                HostMemoryUsagePct = item.HostMemoryUsage,
                DiskSizeGB = ToGB(item.DiskSize),
                DiskUsageGB = ToGB(item.DiskUsage),
                DiskUsagePct = item.DiskUsagePercentage,
                Uptime = FormatHelper.UptimeInfo(item.Uptime),
                d.Config?.Hostname,
                OsVersion = d.Config?.OsTypeDecode,
                NetworksWrap = networks.Select(a => $"{a.Network.MacAddress} {a.Network.Bridge}{(a.Network.Tag.HasValue ? $"/{a.Network.Tag}" : "")}")
                                       .JoinAsString(Environment.NewLine),
                IpAddressesWrap = networks.Select(a => new[] { a.Network.IpAddress, a.Network.IpAddress6 }
                                                        .Where(s => !string.IsNullOrEmpty(s))
                                                        .JoinAsString(", "))
                                          .Where(s => !string.IsNullOrEmpty(s))
                                          .JoinAsString(Environment.NewLine),
                OnBootFlag = ToX(d.Config?.OnBoot),
                ConfigProtectionFlag = ToX(d.Config?.Protection),
                DescriptionWrap = item.Description,
                d.Config?.Cores,
                d.Config?.Swap,
                UnprivilegedFlag = ToX(d.Config?.Unprivileged),
                d.Config?.Nameserver,
                d.Config?.SearchDomain,
                d.Config?.Features,
                d.Config?.Timezone,
                d.Config?.Startup,
                d.Config?.Hookscript,
            });

            if (d.Config != null)
            {
                AppendDiskRows(item, d.Config.Disks);

                if (settings.Guest.Detail.Enabled)
                {
                    await AddContainerDetailAsync(workbook, d, pt);
                }
            }
        }

        sw.CreateTable(null, items, tbl =>
        {
            sw.ApplyNodeLinks(tbl);
            sw.ApplyVmIdLinks(tbl);
        });

        sw.AdjustColumns();

        return resources.Count;
    }

    private async Task AddContainerDetailAsync(XLWorkbook workbook, CtFetchData d, ProgressTracker pt)
    {
        var config = d.Config!;
        var sw = CreateSheetWriter(workbook, GetSheetName(ClusterResourceType.Vm, d.Item.VmId.ToString())!);
        sw.WriteBackLink("Containers", "list:containers");

        var configKv = new Dictionary<string, object?>
        {
            ["On Boot"] = config.OnBoot,
            ["OS Type"] = config.OsTypeDecode,
            ["Protection"] = config.Protection,
            ["Template"] = config.Template,
            ["Lock"] = config.Lock,
            ["Tags"] = ToNewLine(config.Tags, ";"),
            ["Hostname"] = config.Hostname,
            ["Cores"] = config.Cores,
            ["Memory (MB)"] = config.Memory,
            ["Swap (MB)"] = config.Swap,
            ["Unprivileged"] = config.Unprivileged,
            ["Nameserver"] = config.Nameserver,
            ["Search Domain"] = config.SearchDomain,
            ["Features"] = config.Features,
            ["Timezone"] = config.Timezone,
            ["Startup"] = config.Startup,
            ["Hookscript"] = config.Hookscript,
        };

        foreach (var (key, value) in config.ExtensionData.OrderBy(a => a.Key))
        {
            configKv.TryAdd(key, value);
        }

        sw.WriteKeyValue($"{d.Item.VmId} - {d.Item.Name}",
                         new()
                         {
                             ["VM ID"] = d.Item.VmId,
                             ["Name"] = d.Item.Name,
                             ["Hostname"] = config.Hostname,
                             ["Node"] = d.Item.Node,
                             ["Status"] = d.Item.Status,
                             ["CPU"] = d.Item.CpuSize,
                             ["CPU Usage"] = d.Item.HostCpuUsage,
                             ["Memory GB"] = ToGB(d.Item.MemorySize),
                             ["Memory Host %"] = d.Item.HostMemoryUsage,
                             ["Disk GB"] = ToGB(d.Item.DiskSize),
                             ["Uptime"] = FormatHelper.UptimeInfo(d.Item.Uptime),
                         });

        var mainRow = sw.Row;
        sw.Row = 1;
        sw.Col = 4;
        sw.WriteKeyValue("Config", configKv);
        sw.Row = Math.Max(sw.Row, mainRow);
        sw.Col = 1;

        var tableCount = (settings.Firewall.Enabled && settings.Guest.Detail.IncludeFirewallLog ? 1 : 0)  // Firewall Logs
                       + (settings.Guest.Detail.Tasks.Enabled ? 1 : 0);

        sw.ReserveIndexRows(tableCount);

        if (settings.Firewall.Enabled && settings.Guest.Detail.IncludeFirewallLog)
        {
            pt.Step("Firewall Logs");
            AddLogs(sw,
                    "Firewall Logs",
                    await client.Nodes[d.Item.Node]
                                .Lxc[d.Item.VmId]
                                .Firewall
                                .Log
                                .GetAsync(limit: settings.Firewall.Limit,
                                          since: settings.Firewall.SinceUnix,
                                          until: settings.Firewall.UntilUnix));
        }

        await AddGuestTasksTableAsync(sw, pt, d.Item.Node, d.Item.VmId);

        sw.WriteIndex();
        sw.AdjustColumns();
    }
}
