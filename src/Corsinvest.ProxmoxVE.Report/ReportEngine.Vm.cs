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
    private class VmFetchData
    {
        public required ClusterResource Item { get; init; }
        public VmConfigQemu? Config { get; init; }
        public string Hostname { get; init; } = "";
        public string OsVersion { get; init; } = "";
        public bool AgentRunning { get; init; }
        public string AgentVersion { get; init; } = "";
        public VmQemuAgentOsInfo? AgentOsInfo { get; init; }
        public List<VmNetworkRow> Networks { get; init; } = [];
        public IEnumerable<VmQemuAgentGetFsInfo.ResultInt> FsInfo { get; init; } = [];
    }

    private async Task<VmFetchData> FetchVmDataAsync(ClusterResource item, ProgressTracker pt)
    {
        pt.Next(item);

        pt.Step("Config");
        var config = item.IsUnknown
                        ? null
                        : await client.Nodes[item.Node].Qemu[item.VmId].Config.GetAsync();

        VmQemuAgentOsInfo? agentOsInfo = null;
        var hostname = "";
        var osVersion = config?.OsTypeDecode ?? "";
        var agentRunning = false;
        var agentVersion = "";
        var networks = new List<VmNetworkRow>();
        IEnumerable<VmQemuAgentGetFsInfo.ResultInt> fsInfo = [];

        if (!item.IsUnknown && item.IsRunning)
        {
            if (config?.AgentEnabled is not true)
            {
                hostname = "Agent not enabled!";
            }
            else if (settings.Guest.IncludeQemuAgent)
            {
                var vmQemu = client.Nodes[item.Node].Qemu[item.VmId];
                try
                {
                    pt.Step("Agent Info");
                    var agentInfoTask = vmQemu.Agent.Info.GetAsync();
                    var agentResponded = await Task.WhenAny(agentInfoTask, Task.Delay(TimeSpan.FromSeconds(settings.Guest.QemuAgentTimeout))) == agentInfoTask;

                    if (!agentResponded)
                    {
                        hostname = "Agent timeout!";
                    }
                    else
                    {
                        agentVersion = agentInfoTask.Result?.Result?.Version ?? "";
                        agentRunning = true;

                        pt.Step("Agent Data");
                        var osInfoTask = vmQemu.Agent.GetOsinfo.GetAsync();
                        var hostNameTask = vmQemu.Agent.GetHostName.GetAsync();
                        var networkTask = vmQemu.Agent.NetworkGetInterfaces.GetAsync();
                        var fsInfoTask = vmQemu.Agent.GetFsinfo.GetAsync();
                        await Task.WhenAll(osInfoTask, hostNameTask, networkTask, fsInfoTask);

                        agentOsInfo = osInfoTask.Result;
                        osVersion = agentOsInfo?.Result?.OsVersion ?? osVersion;
                        hostname = hostNameTask.Result?.Result?.HostName ?? "";
                        fsInfo = fsInfoTask.Result?.Result ?? [];

                        var agentNetwork = networkTask.Result;
                        if (agentNetwork?.Result != null)
                        {
                            var netDict = config.Networks.Where(n => !string.IsNullOrEmpty(n.MacAddress))
                                                         .ToDictionary(n => n.MacAddress, StringComparer.OrdinalIgnoreCase);

                            foreach (var net in agentNetwork.Result.Where(a => !string.IsNullOrEmpty(a.HardwareAddress)
                                                                                && a.HardwareAddress != "00:00:00:00:00:00"))
                            {
                                netDict.TryGetValue(net.HardwareAddress ?? "", out var configNet);

                                networks.Add(new(item.VmId,
                                                 item.Name,
                                                 item.Node,
                                                 item.Type,
                                                 item.Status,
                                                 hostname,
                                                 new()
                                                 {
                                                     Name = net.Name,
                                                     MacAddress = net.HardwareAddress?.ToUpperInvariant(),
                                                     Bridge = configNet?.Bridge,
                                                     Tag = configNet?.Tag,
                                                     Model = configNet?.Model,
                                                     Firewall = configNet?.Firewall ?? false,
                                                     Gateway = configNet?.Gateway,
                                                     Gateway6 = configNet?.Gateway6,
                                                     Rate = configNet?.Rate,
                                                     Mtu = configNet?.Mtu,
                                                     IpAddress = net.IpAddresses.Where(a => a.IpAddressType == "ipv4")
                                                                                .Select(a => $"{a.IpAddress}/{a.Prefix}")
                                                                                .JoinAsString(Environment.NewLine),
                                                     IpAddress6 = net.IpAddresses.Where(a => a.IpAddressType == "ipv6")
                                                                                 .Select(a => $"{a.IpAddress}/{a.Prefix}")
                                                                                 .JoinAsString(Environment.NewLine),
                                                 },
                                                 IsInternal: configNet == null));
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    hostname = "Agent not running!";
                }
            }
        }

        if (!item.IsUnknown && !agentRunning && config != null)
        {
            foreach (var net in config.Networks)
            {
                networks.Add(new(item.VmId,
                                 item.Name,
                                 item.Node,
                                 item.Type,
                                 item.Status,
                                 hostname,
                                 net,
                                 IsInternal: false));
            }
        }

        return new()
        {
            Item = item,
            Config = config,
            Hostname = hostname,
            OsVersion = osVersion,
            AgentRunning = agentRunning,
            AgentVersion = agentVersion,
            AgentOsInfo = agentOsInfo,
            Networks = networks,
            FsInfo = fsInfo,
        };
    }

    private async Task<int> AddVmsDataAsync(XLWorkbook workbook)
    {
        var sw = CreateSheetWriter(workbook, "Vms");

        var resources = GetResources(ClusterResourceType.Vm)
                                  .Where(a => a.VmType == VmType.Qemu)
                                  .OrderBy(a => a.Id)
                                  .ToList();

        var items = new List<dynamic>();
        var pt = new ProgressTracker(_progress, resources.Count);

        var semaphore = CreateSemaphore();
        var tasks = resources.Select(async item =>
        {
            await semaphore.WaitAsync();
            try { return await FetchVmDataAsync(item, pt); }
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
                d.Hostname,
                OsName = d.AgentOsInfo?.Result?.Name,
                OsVersion = d.AgentOsInfo?.Result?.PrettyName ?? d.OsVersion,
                OsKernelRelease = d.AgentOsInfo?.Result?.KernelRelease,
                AgentEnabledFlag = ToX(d.Config?.AgentEnabled),
                AgentRunningFlag = ToX(d.AgentRunning),
                d.AgentVersion,
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
                d.Config?.Bios,
                d.Config?.Boot,
                d.Config?.Machine,
                d.Config?.Cores,
                d.Config?.Sockets,
                d.Config?.Cpu,
                KvmFlag = ToX(d.Config?.Kvm),
                d.Config?.ScsiHw,
                d.Config?.Vga,
                NumaFlag = ToX(d.Config?.Numa),
                d.Config?.Balloon,
                d.Config?.CpuLimit,
                d.Config?.CpuUnits,
                d.Config?.Hugepages,
                d.Config?.Hookscript,
                d.Config?.StartUp,
            });

            if (!item.IsUnknown && d.Config != null)
            {
                if (d.AgentRunning)
                {
                    AppendPartitionRows(item, d.FsInfo);
                }

                AppendDiskRows(item, d.Config.Disks);

                if (settings.Guest.Detail.Enabled)
                {
                    await AddVmDetailAsync(workbook, d, pt);
                }
            }
        }

        sw.CreateTable(null,
                       items,
                       tbl =>
                       {
                           sw.ApplyNodeLinks(tbl);
                           sw.ApplyVmIdLinks(tbl);
                       });

        sw.AdjustColumns();

        return resources.Count;
    }

    private async Task AddVmDetailAsync(XLWorkbook workbook, VmFetchData d, ProgressTracker pt)
    {
        var config = d.Config!;
        var sw = CreateSheetWriter(workbook, GetSheetName(ClusterResourceType.Vm, d.Item.VmId.ToString())!);
        sw.WriteBackLink("Vms", "list:vms");

        var configKv = new Dictionary<string, object?>
        {
            ["On Boot"] = config.OnBoot,
            ["OS Type"] = config.OsTypeDecode,
            ["Protection"] = config.Protection,
            ["Template"] = config.Template,
            ["Lock"] = config.Lock,
            ["Tags"] = ToNewLine(config.Tags, ";"),
            ["Bios"] = config.Bios,
            ["Boot"] = config.Boot,
            ["Machine"] = config.Machine,
            ["CPU"] = config.Cpu,
            ["Sockets"] = config.Sockets,
            ["Cores"] = config.Cores,
            ["vCPUs"] = config.Vcpus,
            ["CPU Limit"] = config.CpuLimit,
            ["CPU Units"] = config.CpuUnits,
            ["Affinity"] = config.Affinity,
            ["Hotplug"] = config.Hotplug,
            ["Hugepages"] = config.Hugepages,
            ["Memory (MB)"] = config.Memory,
            ["Balloon"] = config.Balloon,
            ["Shares"] = config.Shares,
            ["KVM"] = config.Kvm,
            ["NUMA"] = config.Numa,
            ["Tablet"] = config.Tablet,
            ["ScsiHw"] = config.ScsiHw,
            ["Vga"] = config.Vga,
            ["Agent"] = config.AgentEnabled,
            ["Agent Running"] = d.AgentRunning,
            ["TPM State"] = config.Tpmstate0,
            ["Watchdog"] = config.Watchdog,
            ["Rng0"] = config.Rng0,
            ["Start Up"] = config.StartUp,
            ["Hookscript"] = config.Hookscript,
        };

        foreach (var (key, value) in config.ExtensionData.OrderBy(a => a.Key))
        {
            configKv.TryAdd(key, value);
        }

        pt.Step("QemuAgent");

        sw.WriteKeyValue($"{d.Item.VmId} - {d.Item.Name}",
                         new()
                         {
                             ["VM ID"] = d.Item.VmId,
                             ["Name"] = d.Item.Name,
                             ["Hostname"] = d.Hostname,
                             ["Agent Version"] = d.AgentVersion,
                             ["Node"] = d.Item.Node,
                             ["Status"] = d.Item.Status,
                             ["CPU"] = d.Item.CpuSize,
                             ["CPU Usage %"] = d.Item.HostCpuUsage,
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

        if (d.AgentRunning && d.AgentOsInfo?.Result != null)
        {
            var mainRowOs = sw.Row;
            sw.Row = 1;
            sw.Col = 7;
            sw.WriteKeyValue("Agent OS Info",
                             new Dictionary<string, object?>
                             {
                                 ["Name"] = d.AgentOsInfo.Result.Name,
                                 ["Pretty Name"] = d.AgentOsInfo.Result.PrettyName,
                                 ["Version"] = d.AgentOsInfo.Result.Version,
                                 ["Version Id"] = d.AgentOsInfo.Result.VersionId,
                                 ["Id"] = d.AgentOsInfo.Result.Id,
                                 ["Kernel Release"] = d.AgentOsInfo.Result.KernelRelease,
                                 ["Kernel Version"] = d.AgentOsInfo.Result.KernelVersion,
                                 ["Machine"] = d.AgentOsInfo.Result.Machine,
                                 ["Variant"] = d.AgentOsInfo.Result.Variant,
                                 ["Variant Id"] = d.AgentOsInfo.Result.VariantId,
                             });
            sw.Row = Math.Max(sw.Row, mainRowOs);
            sw.Col = 1;
        }

        var tableCount = (settings.Firewall.Enabled && settings.Guest.Detail.IncludeFirewallLog ? 1 : 0)  // Firewall Logs
                         + (settings.Guest.Detail.Tasks.Enabled ? 1 : 0);

        sw.ReserveIndexRows(tableCount);

        if (settings.Firewall.Enabled && settings.Guest.Detail.IncludeFirewallLog)
        {
            pt.Step("Firewall Logs");
            AddLogs(sw,
                    "Firewall Logs",
                    await client.Nodes[d.Item.Node]
                                .Qemu[d.Item.VmId]
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
