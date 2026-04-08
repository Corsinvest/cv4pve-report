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
    private async Task AddContainersDataAsync(XLWorkbook workbook)
    {
        var sw = CreateSheetWriter(workbook, "Containers");

        var resources = GetResources(ClusterResourceType.Vm)
                                  .Where(a => a.VmType == VmType.Lxc)
                                  .OrderBy(a => a.Id)
                                  .ToList();

        var items = new List<dynamic>();
        var pt = new ProgressTracker(_progress, resources.Count);

        foreach (var item in resources)
        {
            pt.Next(item);

            pt.Step("Config");
            VmConfigLxc? config = item.IsUnknown
                            ? null
                            : await client.Nodes[item.Node].Lxc[item.VmId].Config.GetAsync();

            var ctNetworks = new List<VmNetworkRow>();
            if (config != null)
            {
                foreach (var net in config.Networks)
                {
                    var vmNet = new VmNetworkRow(item.VmId,
                                                 item.Name,
                                                 item.Node,
                                                 item.Type,
                                                 item.Status,
                                                 config.Hostname ?? "",
                                                 net,
                                                 IsInternal: false);
                    ctNetworks.Add(vmNet);
                    AppendVmNetworkRows(workbook, vmNet);
                }
            }

            var networks = ctNetworks.Where(a => !a.IsInternal);

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
                config?.Hostname,
                OsVersion = config?.OsTypeDecode,
                NetworksWrap = networks.Select(a => $"{a.Network.MacAddress} {a.Network.Bridge}{(a.Network.Tag.HasValue ? $"/{a.Network.Tag}" : "")}")
                                       .JoinAsString(Environment.NewLine),
                IpAddressesWrap = networks.Select(a => new[] { a.Network.IpAddress, a.Network.IpAddress6 }
                                                        .Where(s => !string.IsNullOrEmpty(s))
                                                        .JoinAsString(", "))
                                          .Where(s => !string.IsNullOrEmpty(s))
                                          .JoinAsString(Environment.NewLine),
                OnBootFlag = ToX(config?.OnBoot),
                ConfigProtectionFlag = ToX(config?.Protection),
                DescriptionWrap = item.Description,
                config?.Cores,
                config?.Swap,
                UnprivilegedFlag = ToX(config?.Unprivileged),
                config?.Nameserver,
                config?.SearchDomain,
                config?.Features,
                config?.Timezone,
                config?.Startup,
                config?.Hookscript,
            });

            if (config != null)
            {
                pt.Step("Disks");
                AppendDiskRows(workbook, item, config.Disks);

                if (settings.Guest.Detail.Enabled)
                {
                    await AddContainerDetailAsync(workbook, item, config, pt);
                }
            }
        }

        sw.CreateTable(null, items, tbl =>
        {
            sw.ApplyNodeLinks(tbl);
            sw.ApplyVmIdLinks(tbl);
        });

        sw.AdjustColumns();
    }

    private async Task AddContainerDetailAsync(XLWorkbook workbook,
                                               ClusterResource item,
                                               VmConfigLxc config,
                                               ProgressTracker pt)
    {
        var sw = CreateSheetWriter(workbook, GetSheetName(ClusterResourceType.Vm, item.VmId.ToString())!);
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

        sw.WriteKeyValue($"{item.VmId} - {item.Name}",
                         new()
                         {
                             ["VM ID"] = item.VmId,
                             ["Name"] = item.Name,
                             ["Hostname"] = config.Hostname,
                             ["Node"] = item.Node,
                             ["Status"] = item.Status,
                             ["CPU"] = item.CpuSize,
                             ["CPU Usage"] = item.HostCpuUsage,
                             ["Memory GB"] = ToGB(item.MemorySize),
                             ["Memory Host %"] = item.HostMemoryUsage,
                             ["Disk GB"] = ToGB(item.DiskSize),
                             ["Uptime"] = FormatHelper.UptimeInfo(item.Uptime),
                         });

        var mainRow = sw.Row;
        sw.Row = 1;
        sw.Col = 4;
        sw.WriteKeyValue("Config", configKv);
        sw.Row = Math.Max(sw.Row, mainRow);
        sw.Col = 1;

        var tableCount = (settings.Firewall.Enabled ? 1 : 0)  // Firewall Logs
                       + (settings.Guest.Detail.Tasks.Enabled ? 1 : 0);

        sw.ReserveIndexRows(tableCount);

        if (settings.Firewall.Enabled)
        {
            pt.Step("Firewall Logs");
            AddLogs(sw,
                    "Firewall Logs",
                    await client.Nodes[item.Node]
                                .Lxc[item.VmId]
                                .Firewall
                                .Log
                                .GetAsync(limit: settings.Firewall.Limit,
                                          since: settings.Firewall.SinceUnix,
                                          until: settings.Firewall.UntilUnix));
        }

        await AddGuestTasksTableAsync(sw, pt, item.Node, item.VmId);

        sw.WriteIndex();
        sw.AdjustColumns();
    }
}
