/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using System.ComponentModel;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task AddVmsDataAsync(XLWorkbook workbook)
    {
        var sw = new SheetWriter(workbook.Worksheets.Add("Vms"), _sheetLinks);
        var allResources = (await client.GetResourcesAsync(ClusterResourceType.All)).ToList();
        allResources.CalculateHostUsage();

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
            VmQemuAgentNetworkGetInterfaces? vmQemuAgentNetworkGetInterfaces = null;
            var hostname = string.Empty;
            var ipAddresses = string.Empty;
            var osVersion = config?.OsTypeDecode;
            var agentRunning = false;

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
                                agentRunning = (await client.Nodes[item.Node].Qemu[item.VmId].Agent.Ping.Ping()).IsSuccessStatusCode;
                                if (agentRunning)
                                {
                                    try
                                    {
                                        vmQemuAgentOsInfo = await client.Nodes[item.Node].Qemu[item.VmId].Agent.GetOsinfo.GetAsync();
                                        osVersion = vmQemuAgentOsInfo.Result?.OsVersion;
                                        hostname = (await client.Nodes[item.Node].Qemu[item.VmId].Agent.GetHostName.GetAsync())?.Result?.HostName;
                                        vmQemuAgentNetworkGetInterfaces = await client.Nodes[item.Node].Qemu[item.VmId].Agent.NetworkGetInterfaces.GetAsync();

                                        ipAddresses = (vmQemuAgentNetworkGetInterfaces?.Result ?? [])
                                                        .Where(a => !string.IsNullOrEmpty(a.HardwareAddress)
                                                                    && a.HardwareAddress != "00:00:00:00:00:00")
                                                                .Select(a => a.IpAddresses.Select(i => $"{i.IpAddress}/{i.Prefix}").JoinAsString(", "))
                                                                .JoinAsString(Environment.NewLine);
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
                        ipAddresses = configLxc.Networks
                                               .Select(a => a.IpAddress)
                                               .JoinAsString(Environment.NewLine);

                        break;

                    default: throw new InvalidEnumArgumentException();
                }
            }

            items.Add(new
            {
                item.Node,
                item.VmId,
                item.Name,
                item.Description,
                item.Type,
                item.Status,
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
                Uptime = FormatHelper.UptimeInfo(item.Uptime),
                Hostname = hostname,
                IpAddresses = ipAddresses,
                ConfigOnBoot = config?.OnBoot,
                ConfigArch = config?.Arch,
                ConfigOsType = config?.OsType,
                ConfigOsVersion = osVersion,
                ConfigProtection = config?.Protection,
                LxcCores = configLxc?.Cores,
                LxcNameserver = configLxc?.Nameserver,
                LxcSwap = configLxc?.Swap,
                LxcUnprivileged = configLxc?.Unprivileged,
                QemuAgentEnabled = configQemu?.AgentEnabled,
                QemuBios = configQemu?.Bios,
                QemuBoot = configQemu?.Boot,
                QemuCores = configQemu?.Cores,
                QemuCpu = configQemu?.Cpu,
                QemuKvm = configQemu?.Kvm,
                QemuName = configQemu?.Name,
                QemuScsiHw = configQemu?.ScsiHw,
                QemuSockets = configQemu?.Sockets,
                QemuVga = configQemu?.Vga
            });

            if (!item.IsUnknown)
            {
                await AddVmDetailAsync(workbook,
                                       item,
                                       config!,
                                       hostname!,
                                       agentRunning,
                                       vmQemuAgentOsInfo,
                                       vmQemuAgentNetworkGetInterfaces,
                                       pt);
            }
        }

        sw.CreateTable("Vms", items, tbl =>
        {
            sw.ApplyNodeLinks(tbl);
            sw.ApplyVmIdLinks(tbl);
        });
        sw.AdjustColumns();
    }

    private async Task AddVmDetailAsync(XLWorkbook workbook,
                                        ClusterResource item,
                                        VmConfig config,
                                        string hostname,
                                        bool agentRunning,
                                        VmQemuAgentOsInfo? vmQemuAgentOsInfo,
                                        VmQemuAgentNetworkGetInterfaces? vmQemuAgentNetworkGetInterfaces,
                                        ProgressTracker pt)
    {
        var sw = new SheetWriter(workbook.Worksheets.Add(GetSheetName(ClusterResourceType.Vm, item.VmId.ToString())!), _sheetLinks);

        var configKv = new Dictionary<string, object?>
        {
            ["On Boot"] = config.OnBoot,
            ["Arch"] = config.Arch,
            ["OS Type"] = config.OsTypeDecode,
            ["Protection"] = config.Protection,
            ["Template"] = config.Template,
            ["Lock"] = config.Lock,
            ["Tags"] = config.Tags,
        };

        if (config is VmConfigQemu q)
        {
            configKv["Bios"] = q.Bios;
            configKv["Boot"] = q.Boot;
            configKv["Machine"] = q.Machine;
            configKv["CPU"] = q.Cpu;
            configKv["Sockets"] = q.Sockets;
            configKv["Cores"] = q.Cores;
            configKv["Memory (MB)"] = q.Memory;
            configKv["Balloon"] = q.Balloon;
            configKv["KVM"] = q.Kvm;
            configKv["NUMA"] = q.Numa;
            configKv["ScsiHw"] = q.ScsiHw;
            configKv["Vga"] = q.Vga;
            configKv["Agent"] = q.AgentEnabled;
            configKv["Start Up"] = q.StartUp;
            configKv["Hookscript"] = q.Hookscript;
        }
        else if (config is VmConfigLxc l)
        {
            configKv["Hostname"] = l.Hostname;
            configKv["Cores"] = l.Cores;
            configKv["Memory (MB)"] = l.Memory;
            configKv["Swap (MB)"] = l.Swap;
            configKv["Unprivileged"] = l.Unprivileged;
            configKv["Nameserver"] = l.Nameserver;
            configKv["Search Domain"] = l.SearchDomain;
            configKv["Features"] = l.Features;
            configKv["Timezone"] = l.Timezone;
            configKv["Startup"] = l.Startup;
            configKv["Hookscript"] = l.Hookscript;
        }

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
                             ["Type"] = item.Type,
                             ["Node"] = item.Node,
                             ["Status"] = item.Status,
                             ["CPU"] = item.CpuSize,
                             ["CPU Usage"] = item.HostCpuUsage,
                             ["Memory"] = $"{ToGB(item.MemorySize):0.##} GB",
                             ["Memory Host %"] = item.HostMemoryUsage,
                             ["Disk"] = $"{ToGB(item.DiskSize):0.##} GB",
                             ["Uptime"] = FormatHelper.UptimeInfo(item.Uptime),
                         });

        var mainRow = sw.Row;
        sw.Row = 1;
        sw.Col = 4;
        sw.WriteKeyValue("Config", configKv);
        sw.Row = Math.Max(sw.Row, mainRow);
        sw.Col = 1;

        if (agentRunning)
        {
            if (vmQemuAgentOsInfo?.Result != null)
            {
                var osKv = new Dictionary<string, object?>
                {
                    ["Name"] = vmQemuAgentOsInfo.Result.Name,
                    ["Pretty Name"] = vmQemuAgentOsInfo.Result.PrettyName,
                    ["Version"] = vmQemuAgentOsInfo.Result.Version,
                    ["Version Id"] = vmQemuAgentOsInfo.Result.VersionId,
                    ["Id"] = vmQemuAgentOsInfo.Result.Id,
                    ["Kernel Release"] = vmQemuAgentOsInfo.Result.KernelRelease,
                    ["Kernel Version"] = vmQemuAgentOsInfo.Result.KernelVersion,
                    ["Machine"] = vmQemuAgentOsInfo.Result.Machine,
                    ["Variant"] = vmQemuAgentOsInfo.Result.Variant,
                    ["Variant Id"] = vmQemuAgentOsInfo.Result.VariantId,
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
                       + (agentRunning ? 2 : 0)  // Agent Network + Agent Disks
                       + (settings.Guest.RrdData.Enabled ? 1 : 0)
                       + (settings.Guest.IncludeBackups ? 1 : 0)
                       + (settings.Guest.IncludeSnapshots ? 1 : 0)
                       + (settings.Guest.Firewall.Enabled ? 4 : 0)
                       + (settings.Guest.Tasks.Enabled ? 1 : 0);

        sw.ReserveIndexRows(tableCount);

        if (agentRunning)
        {
            pt.Step("Qemu Agent Network");
            sw.CreateTable("Agent Network",
                           (vmQemuAgentNetworkGetInterfaces?.Result ?? [])
                           .Where(a => !string.IsNullOrEmpty(a.HardwareAddress)
                                       && a.HardwareAddress != "00:00:00:00:00:00")
                           .Select(a => new
                           {
                               a.Name,
                               a.HardwareAddress,
                               IpAddresses = a.IpAddresses.Select(i => $"{i.IpAddress}/{i.Prefix}").JoinAsString(", ")
                           }));

            try
            {
                pt.Step("Qemu Agent Disks");
                var agentFs = await client.Nodes[item.Node].Qemu[item.VmId].Agent.GetFsinfo.GetAsync();
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
                       config.Networks.Select(a => new
                       {
                           a.Name,
                           a.Bridge,
                           a.Type,
                           a.Tag,
                           a.Firewall,
                           a.Gateway,
                           a.IpAddress,
                           a.IpAddress6,
                           a.Gateway6,
                           a.MacAddress,
                           a.Model,
                           a.Rate,
                           a.Mtu
                       }),
                       tbl => sw.ApplyBridgeLinks(tbl, item.Node));

        pt.Step("Disks");
        sw.CreateTable("Disks",
                       config.Disks.Select(a => new
                       {
                           a.Id,
                           a.Storage,
                           a.FileName,
                           //a.Size,
                           SizeGB = ToGB(a.SizeBytes),
                           a.Backup
                       }),
                       tbl => sw.ApplyStorageLinks(tbl, item.Node));

        if (settings.Guest.RrdData.Enabled)
        {
            pt.Step("RRD");
            var rrdTimeFrame = settings.Guest.RrdData.TimeFrame.GetValue();
            var rrdConsolidation = settings.Guest.RrdData.Consolidation.GetValue();
            var rrdData = item.VmType == VmType.Qemu
                            ? await client.Nodes[item.Node].Qemu[item.VmId].Rrddata.GetAsync(rrdTimeFrame, rrdConsolidation)
                            : await client.Nodes[item.Node].Lxc[item.VmId].Rrddata.GetAsync(rrdTimeFrame, rrdConsolidation);

            sw.CreateTable("RRD Data",
                           rrdData.Select(a => new
                           {
                               a.TimeDate,
                               NetInMB = ToMB(a.NetIn),
                               NetOutMB = ToMB(a.NetOut),
                               a.CpuUsagePercentage,
                               MemorySizeGB = ToGB(a.MemorySize),
                               MemoryUsageGB = ToGB(a.MemoryUsage),
                               a.MemoryUsagePercentage,
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
                           (await client.Nodes[item.Node].GetBackupsInAllStoragesAsync(Convert.ToInt32(item.VmId)))
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
                           tbl => sw.ApplyStorageLinks(tbl, item.Node));
        }

        if (settings.Guest.IncludeSnapshots)
        {
            pt.Step("Snapshots");
            sw.CreateTable("Snapshots",
                           (await SnapshotHelper.GetSnapshotsAsync(client, item.Node, item.VmType, item.VmId))
                            .Select(a => new
                            {
                                a.Name,
                                a.Description,
                                a.Parent,
                                a.VmStatus,
                                a.Date,
                            }));
        }

        if (settings.Guest.Firewall.Enabled)
        {
            pt.Step("Firewall");
            var fw = settings.Guest.Firewall;
            var fwLogLimit = fw.LogMaxCount > 0 ? fw.LogMaxCount : (int?)null;
            var fwLogSince = fw.LogSince.HasValue ? (int)new DateTimeOffset(fw.LogSince.Value.ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds() : (int?)null;
            var fwLogUntil = fw.LogUntil.HasValue ? (int)new DateTimeOffset(fw.LogUntil.Value.ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds() : (int?)null;

            AddFirewallRules(sw,
                             item.VmType == VmType.Qemu
                                ? await client.Nodes[item.Node].Qemu[item.VmId].Firewall.Rules.GetAsync()
                                : await client.Nodes[item.Node].Lxc[item.VmId].Firewall.Rules.GetAsync());

            AddFirewallAlias(sw,
                             item.VmType == VmType.Qemu
                                ? await client.Nodes[item.Node].Qemu[item.VmId].Firewall.Aliases.GetAsync()
                                : await client.Nodes[item.Node].Lxc[item.VmId].Firewall.Aliases.GetAsync());

            AddFirewallIpSet(sw,
                             item.VmType == VmType.Qemu
                                ? await client.Nodes[item.Node].Qemu[item.VmId].Firewall.Ipset.GetAsync()
                                : await client.Nodes[item.Node].Lxc[item.VmId].Firewall.Ipset.GetAsync());

            AddLogs(sw,
                    "Firewall Logs",
                    item.VmType == VmType.Qemu
                                ? await client.Nodes[item.Node].Qemu[item.VmId].Firewall.Log.GetAsync(limit: fwLogLimit, since: fwLogSince, until: fwLogUntil)
                                : await client.Nodes[item.Node].Lxc[item.VmId].Firewall.Log.GetAsync(limit: fwLogLimit, since: fwLogSince, until: fwLogUntil));
        }

        if (settings.Guest.Tasks.Enabled)
        {
            pt.Step("Tasks");
            var taskSettings = settings.Guest.Tasks;
            sw.CreateTable("Tasks",
                           (await client.Nodes[item.Node].Tasks.GetAsync(
                               vmid: (int)item.VmId,
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
