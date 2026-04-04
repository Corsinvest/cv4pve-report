/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.ComponentModel;
using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private record VmNetworkRow(long VmId,
                                string Name,
                                string Node,
                                string Type,
                                string Status,
                                string Hostname,
                                string OsInfo,
                                VmNetwork Network,
                                bool IsInternal);

    private record VmRuntimeData(ClusterResource Item,
                                 VmConfig Config,
                                 string Hostname,
                                 string AgentVersion,
                                 bool AgentRunning,
                                 IEnumerable<VmNetworkRow> Networks,
                                 VmQemuAgentOsInfo? AgentOsInfo);

    private async Task AddVmsDataAsync(XLWorkbook workbook)
    {
        var sw = CreateSheetWriter(workbook, "Vms");
        var allResources = _resources.ToList();

        var vmIds = (await client.GetVmsAsync(settings.Guest.Ids))
                        .Select(a => a.VmId)
                        .ToHashSet();

        var resources = allResources.Where(a => a.ResourceType == ClusterResourceType.Vm && vmIds.Contains(a.VmId))
                                    .OrderBy(a => a.Node)
                                    .ThenBy(a => a.Type)
                                    .ThenBy(a => a.VmId)
                                    .ToList();

        var items = new List<dynamic>();
        var pt = new ProgressTracker(_progress, resources.Count);

        foreach (var item in resources)
        {
            pt.Next(item);

            var config = item.IsUnknown
                            ? null
                            : item.VmType == VmType.Qemu
                                ? (VmConfig)await client.Nodes[item.Node].Qemu[item.VmId].Config.GetAsync()
                                : await client.Nodes[item.Node].Lxc[item.VmId].Config.GetAsync();

            var configQemu = config as VmConfigQemu;
            var configLxc = config as VmConfigLxc;

            VmQemuAgentOsInfo? vmQemuAgentOsInfo = null;
            var hostname = string.Empty;
            var osVersion = config?.OsTypeDecode;
            var agentRunning = false;
            var agentVersion = string.Empty;
            var agentIpByMac = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (config != null)
            {
                switch (item.VmType)
                {
                    case VmType.Qemu:
                        if (item.IsRunning)
                        {
                            if (!configQemu!.AgentEnabled)
                            {
                                hostname = "Agent not enabled!";
                            }
                            else if (settings.Guest.IncludeQemuAgent)
                            {
                                var vmQemu = client.Nodes[item.Node].Qemu[item.VmId];

                                agentRunning = (await vmQemu.Agent.Ping.Ping()).IsSuccessStatusCode;
                                if (agentRunning)
                                {
                                    try
                                    {
                                        vmQemuAgentOsInfo = await vmQemu.Agent.GetOsinfo.GetAsync();
                                        osVersion = vmQemuAgentOsInfo.Result?.OsVersion;
                                        hostname = (await vmQemu.Agent.GetHostName.GetAsync())?.Result?.HostName;
                                        var vmQemuAgentNetworkGetInterfaces = await vmQemu.Agent.NetworkGetInterfaces.GetAsync();
                                        agentVersion = (await vmQemu.Agent.Info.GetAsync())?.Result?.Version ?? string.Empty;

                                        agentIpByMac = (vmQemuAgentNetworkGetInterfaces?.Result ?? [])
                                                        .Where(a => !string.IsNullOrEmpty(a.HardwareAddress)
                                                                    && a.HardwareAddress != "00:00:00:00:00:00")
                                                        .ToDictionary(
                                                            a => a.HardwareAddress,
                                                            a => a.IpAddresses.Select(i => $"{i.IpAddress}/{i.Prefix}").JoinAsString(", "),
                                                            StringComparer.OrdinalIgnoreCase);

                                        if (vmQemuAgentNetworkGetInterfaces != null)
                                        {
                                            foreach (var net in vmQemuAgentNetworkGetInterfaces.Result.Where(a => !string.IsNullOrEmpty(a.HardwareAddress)
                                                                                                                    && a.HardwareAddress != "00:00:00:00:00:00"))
                                            {
                                                var configNet = config.Networks
                                                                      .FirstOrDefault(c => string.Equals(c.MacAddress, net.HardwareAddress,
                                                                                                         StringComparison.OrdinalIgnoreCase));
                                                _vmNetworkRows.Add(new VmNetworkRow(item.VmId,
                                                                                    item.Name,
                                                                                    item.Node,
                                                                                    item.Type,
                                                                                    item.Status,
                                                                                    hostname ?? string.Empty,
                                                                                    vmQemuAgentOsInfo?.Result?.PrettyName ?? osVersion ?? string.Empty,
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
                                    catch
                                    {
                                        hostname = "Error Agent data!";
                                    }
                                }
                                else
                                {
                                    hostname = "Agent not running!";
                                }
                            }
                        }
                        break;

                    case VmType.Lxc:
                        hostname = configLxc!.Hostname;
                        break;

                    default: throw new InvalidEnumArgumentException();
                }
            }

            if (config != null && !agentRunning)
            {
                foreach (var net in config.Networks)
                {
                    _vmNetworkRows.Add(new VmNetworkRow(item.VmId,
                                                        item.Name,
                                                        item.Node,
                                                        item.Type,
                                                        item.Status,
                                                        hostname ?? string.Empty,
                                                        vmQemuAgentOsInfo?.Result?.PrettyName ?? osVersion ?? string.Empty,
                                                        net,
                                                        IsInternal: false));
                }
            }

            var vmRuntime = new VmRuntimeData(item,
                                              config!,
                                              hostname ?? string.Empty,
                                              agentVersion,
                                              agentRunning,
                                              [.. _vmNetworkRows.Where(a => a.VmId == item.VmId)],
                                              vmQemuAgentOsInfo);

            items.Add(new
            {
                item.Node,
                item.VmId,
                item.Name,
                item.Description,
                item.Type,
                item.Status,
                item.Pool,
                Tags = (item.Tags + string.Empty).Split(',').JoinAsString(Environment.NewLine),
                item.HaState,
                item.Lock,
                item.IsTemplate,
                item.CpuSize,
                item.CpuUsagePercentage,
                item.HostCpuUsage,
                MemorySizeGB = ToGB(item.MemorySize),
                MemoryUsageGB = ToGB(item.MemoryUsage),
                item.MemoryUsagePercentage,
                HostMemoryUsagePercentage = item.HostMemoryUsage,
                DiskSizeGB = ToGB(item.DiskSize),
                DiskUsageGB = ToGB(item.DiskUsage),
                item.DiskUsagePercentage,
                //NetInMB = ToMB(item.NetIn),
                //NetOutMB = ToMB(item.NetOut),
                //DiskReadGB = ToGB(item.DiskRead),
                //DiskWriteGB = ToGB(item.DiskWrite),
                Uptime = FormatHelper.UptimeInfo(item.Uptime),
                vmRuntime.Hostname,

                Networks = vmRuntime.Networks
                            .Where(a => !a.IsInternal)
                            .Select(a => $"{a.Network.MacAddress} {a.Network.Bridge}{(a.Network.Tag.HasValue ? $"/{a.Network.Tag}" : string.Empty)}")
                            .JoinAsString(Environment.NewLine),

                IpAddresses = vmRuntime.Networks
                            .Where(a => !a.IsInternal)
                            .Select(a => new[] { a.Network.IpAddress, a.Network.IpAddress6 }
                                             .Where(s => !string.IsNullOrEmpty(s))
                                             .JoinAsString(", "))
                            .Where(s => !string.IsNullOrEmpty(s))
                            .JoinAsString(Environment.NewLine),

                config?.OnBoot,
                //config?.Arch,
                config?.OsType,
                OsVersion = osVersion,
                ConfigProtection = config?.Protection,
                LxcCores = configLxc?.Cores,
                LxcNameserver = configLxc?.Nameserver,
                LxcSwap = configLxc?.Swap,
                LxcUnprivileged = configLxc?.Unprivileged,
                QemuAgentEnabled = configQemu?.AgentEnabled,
                QemuAgentVersion = agentVersion,
                QemuBios = configQemu?.Bios,
                QemuBoot = configQemu?.Boot,
                QemuMachine = configQemu?.Machine,
                QemuCores = configQemu?.Cores,
                QemuCpu = configQemu?.Cpu,
                QemuKvm = configQemu?.Kvm,
                QemuName = configQemu?.Name,
                QemuScsiHw = configQemu?.ScsiHw,
                QemuSockets = configQemu?.Sockets,
                QemuVga = configQemu?.Vga
            });

            if (!item.IsUnknown) { await AddVmDetailAsync(workbook, vmRuntime, pt); }
        }

        sw.CreateTable("Vms", items, tbl =>
        {
            sw.ApplyNodeLinks(tbl);
            sw.ApplyVmIdLinks(tbl);
        });
        sw.AdjustColumns();

    }

    private async Task AddVmDetailAsync(XLWorkbook workbook,
                                        VmRuntimeData runtime,
                                        ProgressTracker pt)
    {

        var sw = CreateSheetWriter(workbook, GetSheetName(ClusterResourceType.Vm, runtime.Item.VmId.ToString())!);

        var vmQemu = runtime.Item.VmType == VmType.Qemu
                        ? client.Nodes[runtime.Item.Node].Qemu[runtime.Item.VmId]
                        : null;

        var vmLxc = runtime.Item.VmType == VmType.Lxc
                        ? client.Nodes[runtime.Item.Node].Lxc[runtime.Item.VmId]
                        : null;

        var configKv = new Dictionary<string, object?>
        {
            ["On Boot"] = ToX(runtime.Config.OnBoot),
            //["Arch"] = runtime.Config.Arch,
            ["OS Type"] = runtime.Config.OsTypeDecode,
            ["Protection"] = ToX(runtime.Config.Protection),
            ["Template"] = ToX(runtime.Config.Template),
            ["Lock"] = runtime.Config.Lock,
            ["Tags"] = runtime.Config.Tags,
        };

        if ((VmConfig?)runtime.Config is VmConfigQemu qemuConfig)
        {
            configKv["Bios"] = qemuConfig.Bios;
            configKv["Boot"] = qemuConfig.Boot;
            configKv["Machine"] = qemuConfig.Machine;
            configKv["CPU"] = qemuConfig.Cpu;
            configKv["Sockets"] = qemuConfig.Sockets;
            configKv["Cores"] = qemuConfig.Cores;
            configKv["vCPUs"] = qemuConfig.Vcpus;
            configKv["CPU Limit"] = qemuConfig.CpuLimit;
            configKv["CPU Units"] = qemuConfig.CpuUnits;
            configKv["Affinity"] = qemuConfig.Affinity;
            configKv["Hotplug"] = qemuConfig.Hotplug;
            configKv["Hugepages"] = qemuConfig.Hugepages;
            configKv["Memory (MB)"] = qemuConfig.Memory;
            configKv["Balloon"] = qemuConfig.Balloon;
            configKv["Shares"] = qemuConfig.Shares;
            configKv["KVM"] = qemuConfig.Kvm;
            configKv["NUMA"] = qemuConfig.Numa;
            configKv["Tablet"] = qemuConfig.Tablet;
            configKv["ScsiHw"] = qemuConfig.ScsiHw;
            configKv["Vga"] = qemuConfig.Vga;
            configKv["Agent"] = qemuConfig.AgentEnabled;
            configKv["TPM State"] = qemuConfig.Tpmstate0;
            configKv["Watchdog"] = qemuConfig.Watchdog;
            configKv["Rng0"] = qemuConfig.Rng0;
            configKv["Start Up"] = qemuConfig.StartUp;
            configKv["Hookscript"] = qemuConfig.Hookscript;
        }
        else if ((VmConfig?)runtime.Config is VmConfigLxc lxcConfig)
        {
            configKv["Hostname"] = lxcConfig.Hostname;
            configKv["Cores"] = lxcConfig.Cores;
            configKv["Memory (MB)"] = lxcConfig.Memory;
            configKv["Swap (MB)"] = lxcConfig.Swap;
            configKv["Unprivileged"] = lxcConfig.Unprivileged;
            configKv["Nameserver"] = lxcConfig.Nameserver;
            configKv["Search Domain"] = lxcConfig.SearchDomain;
            configKv["Features"] = lxcConfig.Features;
            configKv["Timezone"] = lxcConfig.Timezone;
            configKv["Startup"] = lxcConfig.Startup;
            configKv["Hookscript"] = lxcConfig.Hookscript;
        }

        foreach (var (key, value) in runtime.Config.ExtensionData.OrderBy(a => a.Key))
        {
            configKv.TryAdd(key, value);
        }

        pt.Step("QemuAgent");

        sw.WriteKeyValue($"{runtime.Item.VmId} - {runtime.Item.Name}",
                         new()
                         {
                             ["VM ID"] = runtime.Item.VmId,
                             ["Name"] = runtime.Item.Name,
                             ["Hostname"] = runtime.Hostname,
                             ["Agent Version"] = runtime.AgentVersion,
                             ["Type"] = runtime.Item.Type,
                             ["Node"] = runtime.Item.Node,
                             ["Status"] = runtime.Item.Status,
                             ["CPU"] = runtime.Item.CpuSize,
                             ["CPU Usage"] = runtime.Item.HostCpuUsage,
                             ["Memory"] = $"{ToGB(runtime.Item.MemorySize):0.##} GB",
                             ["Memory Host %"] = runtime.Item.HostMemoryUsage,
                             ["Disk"] = $"{ToGB(runtime.Item.DiskSize):0.##} GB",
                             ["Uptime"] = FormatHelper.UptimeInfo(runtime.Item.Uptime),
                         });

        var mainRow = sw.Row;
        sw.Row = 1;
        sw.Col = 4;
        sw.WriteKeyValue("Config", configKv);
        sw.Row = Math.Max(sw.Row, mainRow);
        sw.Col = 1;

        if (runtime.AgentRunning)
        {
            if (runtime.AgentOsInfo?.Result != null)
            {
                var osKv = new Dictionary<string, object?>
                {
                    ["Name"] = runtime.AgentOsInfo.Result.Name,
                    ["Pretty Name"] = runtime.AgentOsInfo.Result.PrettyName,
                    ["Version"] = runtime.AgentOsInfo.Result.Version,
                    ["Version Id"] = runtime.AgentOsInfo.Result.VersionId,
                    ["Id"] = runtime.AgentOsInfo.Result.Id,
                    ["Kernel Release"] = runtime.AgentOsInfo.Result.KernelRelease,
                    ["Kernel Version"] = runtime.AgentOsInfo.Result.KernelVersion,
                    ["Machine"] = runtime.AgentOsInfo.Result.Machine,
                    ["Variant"] = runtime.AgentOsInfo.Result.Variant,
                    ["Variant Id"] = runtime.AgentOsInfo.Result.VariantId,
                };

                var mainRowOs = sw.Row;
                sw.Row = 1;
                sw.Col = 7;
                sw.WriteKeyValue("Agent OS Info", osKv);
                sw.Row = Math.Max(sw.Row, mainRowOs);
                sw.Col = 1;
            }
        }

        var tableCount = 2  // Network + Disks
                       + (runtime.AgentRunning ? 2 : 0)  // Agent Network + Agent Disks
                       + (settings.Guest.RrdData.Enabled ? 1 : 0)
                       + (settings.Guest.IncludeBackups ? 1 : 0)
                       + (settings.Guest.IncludeSnapshots ? 1 : 0)
                       + (settings.Guest.Firewall.Enabled ? 4 : 0)
                       + (settings.Guest.Tasks.Enabled ? 1 : 0);

        sw.ReserveIndexRows(tableCount);

        if (runtime.AgentRunning)
        {
            pt.Step("Qemu Agent Network");
            sw.CreateTable("Agent Network",
                           runtime.Networks.Select(a => new
                           {
                               a.Network.MacAddress,
                               a.Network.IpAddress,
                               a.Network.IpAddress6,
                           }));

            try
            {
                pt.Step("Qemu Agent Disks");
                var agentFs = await vmQemu!.Agent.GetFsinfo.GetAsync();
                sw.CreateTable("Agent Disks",
                               (agentFs?.Result ?? []).Select(a => new
                               {
                                   a.Name,
                                   a.MountPoint,
                                   a.Type,
                                   TotalGB = ToGB(a.TotalBytes),
                                   UsedGB = ToGB(a.UsedBytes),
                               }));
            }
            catch { }
        }

        pt.Step("Network");
        sw.CreateTable("Network",
                       runtime.Config.Networks.Select(a => new
                       {
                           a.Name,
                           a.MacAddress,
                           a.Bridge,
                           a.Tag,
                           a.Type,
                           a.Model,
                           a.Firewall,
                           a.IpAddress,
                           a.IpAddress6,
                           a.Gateway,
                           a.Gateway6,
                           a.Rate,
                           a.Mtu,
                           a.Trunks,
                           a.Disconnect,
                           a.LinkDown,
                       }));

        pt.Step("Disks");
        foreach (var disk in runtime.Config.Disks)
        {
            _vmDiskRows.Add(new VmDiskRow(runtime.Item.VmId,
                                          runtime.Item.Name,
                                          runtime.Item.Node,
                                          runtime.Item.Type,
                                          runtime.Item.Status,
                                          disk));
        }

        sw.CreateTable("Disks",
                       runtime.Config.Disks.Select(a => new
                       {
                           a.Id,
                           a.Storage,
                           a.FileName,
                           a.Prealloc,
                           a.Format,
                           SizeGB = ToGB(a.SizeBytes),
                           a.Backup,
                           a.IsUnused,
                           a.Cache,
                           a.Device,
                           a.MountPoint,
                           a.MountSourcePath,
                           a.Passthrough,
                       }),
                       tbl => sw.ApplyStorageLinks(tbl, runtime.Item.Node));

        if (settings.Guest.RrdData.Enabled)
        {
            pt.Step("RRD");
            var rrdTimeFrame = settings.Guest.RrdData.TimeFrame.GetValue();
            var rrdConsolidation = settings.Guest.RrdData.Consolidation.GetValue();
            var rrdData = vmQemu != null
                            ? await vmQemu.Rrddata.GetAsync(rrdTimeFrame, rrdConsolidation)
                            : await vmLxc!.Rrddata.GetAsync(rrdTimeFrame, rrdConsolidation);

            sw.CreateTable("RRD Data",
                           rrdData.Select(a => new
                           {
                               a.TimeDate,
                               a.CpuUsagePercentage,
                               MemorySizeGB = ToGB(a.MemorySize),
                               MemoryUsageGB = ToGB(a.MemoryUsage),
                               a.MemoryUsagePercentage,
                               NetInMB = ToMB(a.NetIn),
                               NetOutMB = ToMB(a.NetOut),
                               DiskReadMB = ToMB(a.DiskRead),
                               DiskWriteMB = ToMB(a.DiskWrite),
                               DiskSizeGB = ToGB(a.DiskSize),
                               DiskUsageGB = ToGB(a.DiskUsage),
                               a.DiskUsagePercentage,
                               PsiCpuSomePercentage = a.PressureCpuSome,
                               PsiCpuFullPercentage = a.PressureCpuFull,
                               PsiIoSomePercentage = a.PressureIoSome,
                               PsiIoFullPercentage = a.PressureIoFull,
                               PsiMemSomePercentage = a.PressureMemorySome,
                               PsiMemFullPercentage = a.PressureMemoryFull,
                           }));
        }

        if (settings.Guest.IncludeBackups)
        {
            pt.Step("Backups");
            sw.CreateTable("Backup",
                           (await client.Nodes[runtime.Item.Node].GetBackupsInAllStoragesAsync((int)runtime.Item.VmId))
                            .Select(a => new
                            {
                                a.ContentDescription,
                                a.CreationDate,
                                a.Encrypted,
                                a.FileName,
                                a.Format,
                                a.Notes,
                                a.Protected,
                                SizeGB = ToGB(a.Size),
                                a.Storage,
                                a.Verified
                            }),
                           tbl => sw.ApplyStorageLinks(tbl, runtime.Item.Node));
        }

        if (settings.Guest.IncludeSnapshots)
        {
            pt.Step("Snapshots");
            sw.CreateTable("Snapshots",
                           (await SnapshotHelper.GetSnapshotsAsync(client, 
                                                                   runtime.Item.Node, 
                                                                   runtime.Item.VmType, 
                                                                   runtime.Item.VmId))
                            .Select(a => new
                            {
                                a.Name,
                                a.Parent,
                                a.Date,
                                IncludeRam = a.VmStatus,
                                a.Description,
                            }));
        }

        if (settings.Guest.Firewall.Enabled)
        {
            pt.Step("Firewall");
            var fw = settings.Guest.Firewall;

            var fwLogLimit = fw.LogMaxCount > 0 ? fw.LogMaxCount : (int?)null;

            var fwLogSince = fw.LogSince.HasValue
                                ? (int)new DateTimeOffset(fw.LogSince.Value.ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds()
                                : (int?)null;

            var fwLogUntil = fw.LogUntil.HasValue
                                ? (int)new DateTimeOffset(fw.LogUntil.Value.ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds()
                                : (int?)null;

            AddFirewallRules(sw,
                             vmQemu != null
                                ? await vmQemu.Firewall.Rules.GetAsync()
                                : await vmLxc!.Firewall.Rules.GetAsync());

            AddFirewallAlias(sw,
                             vmQemu != null
                                ? await vmQemu.Firewall.Aliases.GetAsync()
                                : await vmLxc!.Firewall.Aliases.GetAsync());

            AddFirewallIpSet(sw,
                             vmQemu != null
                                ? await vmQemu.Firewall.Ipset.GetAsync()
                                : await vmLxc!.Firewall.Ipset.GetAsync());

            AddLogs(sw,
                    "Firewall Logs",
                    vmQemu != null
                        ? await vmQemu.Firewall.Log.GetAsync(limit: fwLogLimit, since: fwLogSince, until: fwLogUntil)
                        : await vmLxc!.Firewall.Log.GetAsync(limit: fwLogLimit, since: fwLogSince, until: fwLogUntil));
        }

        if (settings.Guest.Tasks.Enabled)
        {
            pt.Step("Tasks");
            var taskSettings = settings.Guest.Tasks;
            sw.CreateTable("Tasks",
                           (await client.Nodes[runtime.Item.Node].Tasks.GetAsync(
                               vmid: (int)runtime.Item.VmId,
                               errors: taskSettings.OnlyErrors ? true : null,
                               limit: taskSettings.MaxCount > 0 ? taskSettings.MaxCount : null
                           )).Select(a => new
                           {
                               a.UniqueTaskId,
                               a.Type,
                               a.User,
                               a.Status,
                               a.StatusOk,
                               StartTime = a.StartTimeDate,
                               EndTime = a.EndTimeDate,
                               a.Duration
                           }));
        }

        sw.WriteIndex();
        sw.AdjustColumns();
    }
}
