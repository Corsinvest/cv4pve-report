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
    private async Task AddVmsDataAsync(XLWorkbook workbook)
    {
        var sw = CreateSheetWriter(workbook, "Vms");

        var resources = GetResources(ClusterResourceType.Vm)
                                  .Where(a => a.VmType == VmType.Qemu)
                                  .OrderBy(a => a.Id)
                                  .ToList();

        var items = new List<dynamic>();
        var pt = new ProgressTracker(_progress, resources.Count);

        foreach (var item in resources)
        {
            pt.Next(item);

            pt.Step("Config");
            VmConfigQemu? config = item.IsUnknown
                            ? null
                            : await client.Nodes[item.Node].Qemu[item.VmId].Config.GetAsync();

            VmQemuAgentOsInfo? vmQemuAgentOsInfo = null;
            var hostname = "";
            var osVersion = config?.OsTypeDecode;
            var agentRunning = false;
            var agentVersion = "";
            var vmNetworks = new List<VmNetworkRow>();

            if (!item.IsUnknown && item.IsRunning)
            {
                if (config?.AgentEnabled != true)
                {
                    hostname = "Agent not enabled!";
                }
                else if (settings.Guest.IncludeQemuAgent)
                {
                    var vmQemu = client.Nodes[item.Node].Qemu[item.VmId];

                    try
                    {
                        pt.Step("Agent Info");
                        agentVersion = (await vmQemu.Agent.Info.GetAsync())?.Result?.Version ?? "";
                        agentRunning = true;
                        vmQemuAgentOsInfo = await vmQemu.Agent.GetOsinfo.GetAsync();
                        osVersion = vmQemuAgentOsInfo.Result?.OsVersion;
                        hostname = (await vmQemu.Agent.GetHostName.GetAsync())?.Result?.HostName;
                        var vmQemuAgentNetworkGetInterfaces = await vmQemu.Agent.NetworkGetInterfaces.GetAsync();

                        if (vmQemuAgentNetworkGetInterfaces != null)
                        {
                            foreach (var net in vmQemuAgentNetworkGetInterfaces.Result.Where(a => !string.IsNullOrEmpty(a.HardwareAddress)
                                                                                                    && a.HardwareAddress != "00:00:00:00:00:00"))
                            {
                                var configNet = config.Networks
                                                      .FirstOrDefault(c => string.Equals(c.MacAddress, net.HardwareAddress,
                                                                                         StringComparison.OrdinalIgnoreCase));
                                var vmNet = new VmNetworkRow(item.VmId,
                                                             item.Name,
                                                             item.Node,
                                                             item.Type,
                                                             item.Status,
                                                             hostname ?? "",
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
                                                             IsInternal: configNet == null);
                                vmNetworks.Add(vmNet);
                                AppendVmNetworkRows(workbook, vmNet);
                            }
                        }
                    }
                    catch
                    {
                        hostname = "Agent not running!";
                    }
                }
            }

            if (!item.IsUnknown && !agentRunning && config != null)
            {
                foreach (var net in config.Networks)
                {
                    var vmNet = new VmNetworkRow(item.VmId,
                                                 item.Name,
                                                 item.Node,
                                                 item.Type,
                                                 item.Status,
                                                 hostname ?? "",
                                                 net,
                                                 IsInternal: false);
                    vmNetworks.Add(vmNet);
                    AppendVmNetworkRows(workbook, vmNet);
                }
            }

            var networks = vmNetworks.Where(a => !a.IsInternal);

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
                Hostname = hostname,
                OsName = vmQemuAgentOsInfo?.Result?.Name,
                OsVersion = vmQemuAgentOsInfo?.Result?.PrettyName ?? osVersion,
                OsKernelRelease = vmQemuAgentOsInfo?.Result?.KernelRelease,
                AgentEnabledFlag = ToX(config?.AgentEnabled),
                AgentRunningFlag = ToX(agentRunning),
                AgentVersion = agentVersion,
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
                config?.Bios,
                config?.Boot,
                config?.Machine,
                config?.Cores,
                config?.Sockets,
                config?.Cpu,
                KvmFlag = ToX(config?.Kvm),
                config?.ScsiHw,
                config?.Vga,
                NumaFlag = ToX(config?.Numa),
                config?.Balloon,
                config?.CpuLimit,
                config?.CpuUnits,
                config?.Hugepages,
                config?.Hookscript,
                config?.StartUp,
            });

            if (!item.IsUnknown && config != null)
            {
                if (agentRunning)
                {
                    try
                    {
                        pt.Step("Partitions");
                        AppendPartitionRows(workbook,
                                            item,
                                            (await client.Nodes[item.Node]
                                                         .Qemu[item.VmId]
                                                         .Agent
                                                         .GetFsinfo
                                                         .GetAsync())?
                                                         .Result ?? []);
                    }
                    catch { }
                }

                pt.Step("Disks");
                AppendDiskRows(workbook, item, config.Disks);

                await AddVmDetailAsync(workbook,
                                       item,
                                       config,
                                       hostname ?? "",
                                       agentVersion,
                                       agentRunning,
                                       vmQemuAgentOsInfo,
                                       pt);
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
    }

    private async Task AddVmDetailAsync(XLWorkbook workbook,
                                        ClusterResource item,
                                        VmConfigQemu config,
                                        string hostname,
                                        string agentVersion,
                                        bool agentRunning,
                                        VmQemuAgentOsInfo? agentOsInfo,
                                        ProgressTracker pt)
    {
        var sw = CreateSheetWriter(workbook, GetSheetName(ClusterResourceType.Vm, item.VmId.ToString())!);
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
            ["Agent Running"] = agentRunning,
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

        sw.WriteKeyValue($"{item.VmId} - {item.Name}",
                         new()
                         {
                             ["VM ID"] = item.VmId,
                             ["Name"] = item.Name,
                             ["Hostname"] = hostname,
                             ["Agent Version"] = agentVersion,
                             ["Node"] = item.Node,
                             ["Status"] = item.Status,
                             ["CPU"] = item.CpuSize,
                             ["CPU Usage %"] = item.HostCpuUsage,
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

        if (agentRunning && agentOsInfo?.Result != null)
        {

            var mainRowOs = sw.Row;
            sw.Row = 1;
            sw.Col = 7;
            sw.WriteKeyValue("Agent OS Info",
                             new Dictionary<string, object?>
                             {
                                 ["Name"] = agentOsInfo.Result.Name,
                                 ["Pretty Name"] = agentOsInfo.Result.PrettyName,
                                 ["Version"] = agentOsInfo.Result.Version,
                                 ["Version Id"] = agentOsInfo.Result.VersionId,
                                 ["Id"] = agentOsInfo.Result.Id,
                                 ["Kernel Release"] = agentOsInfo.Result.KernelRelease,
                                 ["Kernel Version"] = agentOsInfo.Result.KernelVersion,
                                 ["Machine"] = agentOsInfo.Result.Machine,
                                 ["Variant"] = agentOsInfo.Result.Variant,
                                 ["Variant Id"] = agentOsInfo.Result.VariantId,
                             });
            sw.Row = Math.Max(sw.Row, mainRowOs);
            sw.Col = 1;
        }

        var tableCount = (settings.Firewall.Enabled ? 1 : 0)  // Firewall Logs
                         + (settings.Guest.Tasks.Enabled ? 1 : 0);

        sw.ReserveIndexRows(tableCount);

        if (settings.Firewall.Enabled)
        {
            pt.Step("Firewall Logs");

            AddLogs(sw,
                    "Firewall Logs",
                    await client.Nodes[item.Node]
                                .Qemu[item.VmId]
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
