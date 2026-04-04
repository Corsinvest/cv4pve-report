/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task AddNodesDataAsync(XLWorkbook workbook)
    {
        var sw = CreateSheetWriter(workbook, "Nodes");
        var items = new List<dynamic>();

        var filtered = _resources.Where(a => a.ResourceType == ClusterResourceType.Node && CheckNames(settings.Node.Names, a.Node))
                                 .OrderBy(a => a.Node)
                                 .ToList();

        var pt = new ProgressTracker(_progress, filtered.Count);

        foreach (var item in filtered)
        {
            pt.Next(item);

            var status = item.IsUnknown
                            ? null
                            : await client.Nodes[item.Node].Status.GetAsync();

            var version = item.IsUnknown
                            ? null
                            : await client.Nodes[item.Node].Version.GetAsync();

            var subscription = item.IsUnknown
                                ? null
                                : await client.Nodes[item.Node].Subscription.GetAsync();

            items.Add(new
            {
                item.Node,
                item.Status,
                item.CpuSize,
                MemorySizeGB = ToGB(item.MemorySize),
                MemoryUsageGB = ToGB(item.MemoryUsage),
                item.MemoryUsagePercentage,
                DiskSizeGB = ToGB(item.DiskSize),
                item.DiskUsagePercentage,
                Uptime = FormatHelper.UptimeInfo(item.Uptime),
                item.CgroupMode,
                item.NodeLevel,
                CpuCpus = status?.CpuInfo.Cpus,
                CpuModel = status?.CpuInfo.Model,
                CpuMhz = status?.CpuInfo.Mhz,
                CpuCores = status?.CpuInfo.Cores,
                CpuSockets = status?.CpuInfo.Sockets,
                SwapTotalGB = ToGB(status?.Swap.Total ?? 0),
                SwapUsedGB = ToGB(status?.Swap.Used ?? 0),
                status?.PveVersion,
                RootFsTotalGB = ToGB(status?.RootFs.Total ?? 0),
                RootFsUsedGB = ToGB(status?.RootFs.Used ?? 0),
                KernelVersion = status?.Kversion,
                SubscriptionProductName = subscription?.ProductName,
                SubscriptionRegDate = subscription?.RegDate,
                VersionRelease = version?.Release,
                VersionVersion = version?.Version,
            });

            if (!item.IsUnknown)
            {
                await AddNodeDetailAsync(workbook,
                                         item,
                                         status!,
                                         version!,
                                         subscription!,
                                         pt);
            }
        }

        sw.CreateTable("Nodes",
                       items,
                       tbl => sw.ApplyColumnLinks(tbl, "Node", cell => $"node:{cell.Value}"));
        sw.AdjustColumns();
    }

    private async Task AddNodeDetailAsync(XLWorkbook workbook,
                                          ClusterResource item,
                                          NodeStatus status,
                                          NodeVersion version,
                                          NodeSubscription subscription,
                                          ProgressTracker pt)
    {
        var node = item.Node;
        var sw = CreateSheetWriter(workbook, GetSheetName(ClusterResourceType.Node, node)!);

        sw.WriteKeyValue(node,
                         new()
                         {
                             ["Status"] = item.Status,
                             ["CPU Sockets"] = status.CpuInfo.Sockets,
                             ["CPU Cores"] = status.CpuInfo.Cores,
                             ["CPU Model"] = status.CpuInfo.Model,
                             ["CPU MHz"] = status.CpuInfo.Mhz,
                             ["Memory"] = $"{ToGB(status.Memory.Total):0.##} GB",
                             ["Memory Used"] = $"{ToGB(status.Memory.Used):0.##} GB",
                             ["Swap"] = $"{ToGB(status.Swap.Total):0.##} GB",
                             ["Root FS"] = $"{ToGB(status.RootFs.Total):0.##} GB",
                             ["Kernel"] = status.Kversion,
                             ["PVE Version"] = status.PveVersion,
                             ["Version"] = $"{version.Version}-{version.Release}",
                             ["Subscription"] = subscription.ProductName,
                             ["Subscription Expiry"] = subscription.RegDate,
                         });

        var tableCount = 1  // Services
                       + 1  // Network
                       + (settings.Node.Disk.Enabled ? 1 : 0)
                       + (settings.Node.Disk.IncludeSmartData ? 1 : 0)
                       + (settings.Node.Disk.IncludeZfs ? 2 : 0)
                       + (settings.Node.Disk.IncludeDirectory ? 1 : 0)
                       + (settings.Node.IncludeReplication ? 1 : 0)
                       + (settings.Node.RrdData.Enabled ? 1 : 0)
                       + (settings.Node.IncludeAptRepositories ? 1 : 0)
                       + (settings.Node.IncludeAptUpdates ? 1 : 0)
                       + (settings.Node.IncludeAptVersions ? 1 : 0)
                       + (settings.Node.Firewall.Enabled ? 2 : 0)
                       + (settings.Node.IncludeSslCertificates ? 1 : 0)
                       + (settings.Node.Tasks.Enabled ? 1 : 0);

        sw.ReserveIndexRows(tableCount);

        pt.Step("Services");
        sw.CreateTable("Services",
                       (await client.Nodes[node].Services.GetAsync())
                       .Select(a => new
                       {
                           a.Name,
                           a.Description,
                           a.State,
                           a.ActiveState,
                           a.UnitState,
                           a.Service,
                       }));

        pt.Step("Network");
        var nodeNets = await client.Nodes[node].Network.GetAsync();
        _nodeNetworks[node] = nodeNets;
        sw.CreateTable("Network",
                       nodeNets.Select(a => new
                       {
                           a.Active,
                           a.AutoStart,
                           a.Exists,
                           a.Type,
                           a.Interface,
                           a.LinkType,
                           a.Method,
                           a.Cidr,
                           a.Address,
                           a.Netmask,
                           a.Gateway,
                           a.Method6,
                           a.Cidr6,
                           a.Address6,
                           a.Netmask6,
                           a.Gateway6,
                           a.Priority,
                           a.Mtu,
                           a.BondMode,
                           a.BondMiimon,
                           a.BondPrimary,
                           a.BondXmitHashPolicy,
                           a.Slaves,
                           a.BridgeStp,
                           a.BridgeVlanAware,
                           a.BridgeVids,
                           a.BridgeFd,
                           a.BridgePorts,
                           a.VlanId,
                           a.VlanRawDevice,
                           a.VlanProtocol,
                           a.OvsBridge,
                           a.OvsBonds,
                           a.OvsPorts,
                           a.OvsOptions,
                           a.OvsTag,
                           a.VxlanId,
                           a.VxlanLocalTunnelIp,
                           a.VxlanPhysDev,
                           a.Comments,
                           a.Comments6,
                       }),
                       tbl => sw.RegisterNetworkLinks(tbl, node));

        if (settings.Node.Disk.Enabled || settings.Node.Disk.IncludeSmartData)
        {
            var disksData = await client.Nodes[node].Disks.List.GetAsync(include_partitions: true);

            if (settings.Node.Disk.Enabled)
            {
                pt.Step("Disks");
                sw.CreateTable("Disks",
                               disksData.OrderBy(a => a.DevPath)
                                        .Select(a => new
                                        {
                                            DevicePath = $"{new string(' ', string.IsNullOrEmpty(a.Parent) ? 0 : 2)}{a.DevPath}",
                                            Used = $"{new string(' ', string.IsNullOrEmpty(a.Parent) ? 0 : 2)}{a.Used}",
                                            Type = $"{new string(' ', string.IsNullOrEmpty(a.Parent) ? 0 : 2)}{a.Type}",
                                            a.Vendor,
                                            a.Serial,
                                            a.Model,
                                            a.Wwn,
                                            a.Health,
                                            a.Gpt,
                                            a.Wearout,
                                            a.Rpm,
                                            SizeGB = ToGB(a.Size),
                                            a.Mounted,
                                            a.ByIdLink,
                                            a.OsdId,
                                        }));
            }

            if (settings.Node.Disk.IncludeSmartData)
            {
                pt.Step("SMART Data");
                var smartItems = new List<dynamic>();
                foreach (var disk in disksData.Where(a => string.IsNullOrEmpty(a.Parent)))
                {
                    var smart = await client.GetDiskSmart(node, disk.DevPath);
                    foreach (var attr in smart.Attributes ?? [])
                    {
                        smartItems.Add(new
                        {
                            Disk = disk.DevPath,
                            disk.Model,
                            DiskType = disk.Type,
                            DiskHealth = disk.Health,
                            SmartHealth = smart.Health,
                            attr.Id,
                            attr.Name,
                            attr.Value,
                            attr.Worst,
                            attr.Threshold,
                            attr.Flags,
                            attr.Raw,
                            attr.Fail,
                        });
                    }
                }
                sw.CreateTable("SMART Data", smartItems);
            }
        }

        if (settings.Node.Disk.IncludeDirectory)
        {
            pt.Step("Directory");
            sw.CreateTable("Directory",
                           (await client.Nodes[node].Disks.Directory.GetAsync())
                            .Select(a => new
                            {
                                a.Device,
                                a.Path,
                                a.Type,
                                a.Options,
                                a.UnitFile
                            }));
        }

        if (settings.Node.Disk.IncludeZfs)
        {
            pt.Step("ZFS Pools");

            var zfsPools = new List<dynamic>();
            var zfsPoolsStatus = new List<dynamic>();

            foreach (var pool in await client.Nodes[node].Disks.Zfs.GetAsync())
            {
                var poolData = await client.Nodes[node].Disks.Zfs[pool.Name].GetAsync();

                zfsPools.Add(new
                {
                    pool.Name,
                    SizeGB = ToGB(pool.Size),
                    FreeGB = ToGB(pool.Free),
                    AllocatedGB = ToGB(pool.Alloc),
                    FragmentationPercentage = pool.Frag / 100.0,
                    Deduplication = pool.Dedup,
                    pool.Health,
                    poolData.Scan,
                    poolData.Status,
                    poolData.Action,
                    poolData.Errors,
                });

                zfsPoolsStatus.AddRange(MakeZfsStatus(pool.Name, poolData.Children, null, 0));
            }

            sw.CreateTable("ZFS Pools", zfsPools);
            sw.CreateTable("ZFS Pool Status", zfsPoolsStatus);
        }

        if (settings.Node.IncludeReplication)
        {
            pt.Step("Replication");
            sw.CreateTable("Replication",
                           (await client.Nodes[node].Replication.GetAsync())
                            .Select(a => new
                            {
                                a.Id,
                                a.Type,
                                a.VmType,
                                a.Guest,
                                a.Source,
                                a.Target,
                                a.Schedule,
                                a.JobNum,
                                a.Disable,
                                a.FailCount,
                                a.Duration,
                                a.Rate,
                                LastSync = FromUnixTime(a.LastSync),
                                NextSync = FromUnixTime(a.NextSync),
                                LastTry = FromUnixTime(a.LastTry),
                                a.Error,
                                a.Comment,
                            }),
                           tbl => sw.ApplyReplicationLinks(tbl));
        }

        if (settings.Node.RrdData.Enabled)
        {
            pt.Step("RRD");
            sw.CreateTable("RRD Data",
                           (await client.Nodes[node].Rrddata.GetAsync(settings.Node.RrdData.TimeFrame.GetValue(),
                                                                      settings.Node.RrdData.Consolidation.GetValue()))
                                .Select(a => new
                                {
                                    a.TimeDate,
                                    NetInMB = ToMB(a.NetIn),
                                    NetOutMB = ToMB(a.NetOut),
                                    a.CpuUsagePercentage,
                                    a.IoWait,
                                    a.Loadavg,
                                    MemorySizeGB = ToGB(a.MemorySize),
                                    MemoryUsageGB = ToGB(a.MemoryUsage),
                                    a.MemoryUsagePercentage,
                                    SwapSizeGB = ToGB(a.SwapSize),
                                    SwapUsageGB = ToGB(a.SwapUsage),
                                    RootSizeGB = ToGB(a.RootSize),
                                    RootUsageGB = ToGB(a.RootUsage),
                                    PsiCpuSomePercentage = a.PressureCpuSome,
                                    PsiIoSomePercentage = a.PressureIoSome,
                                    PsiIoFullPercentage = a.PressureIoFull,
                                    PsiMemSomePercentage = a.PressureMemorySome,
                                    PsiMemFullPercentage = a.PressureMemoryFull
                                }));
        }

        if (settings.Node.IncludeAptRepositories)
        {
            pt.Step("Apt Repository");
            var aptRepositories = await client.Nodes[node].Apt.Repositories.GetAsync();
            sw.CreateTable("Apt Repository",
                           aptRepositories.Files.SelectMany(a => a.Repositories,
                             (file, repo) => new
                             {
                                 FilePath = file.Path,
                                 file.FileType,
                                 repo.Enabled,
                                 Types = repo.Types.JoinAsString(Environment.NewLine),
                                 URIs = repo.URIs.JoinAsString(Environment.NewLine),
                                 Suites = repo.Suites.JoinAsString(Environment.NewLine),
                                 Components = repo.Components.JoinAsString(Environment.NewLine),
                                 repo.Comment,
                             }));

        }

        if (settings.Node.IncludeAptUpdates)
        {
            pt.Step("Apt Updates");
            sw.CreateTable("Apt Update",
                           (await client.Nodes[node].Apt.Update.GetAsync())
                            .Select(a => new
                            {
                                a.Arch,
                                a.Origin,
                                a.Section,
                                a.Package,
                                a.Priority,
                                a.Version,
                                a.OldVersion,
                                a.Title,
                                a.Description
                            }));
        }

        if (settings.Node.IncludeAptVersions)
        {
            pt.Step("Apt Versions");
            sw.CreateTable("Package Version",
                           (await client.Nodes[node].Apt.Versions.GetAsync())
                            .Select(a => new
                            {
                                a.Arch,
                                a.Origin,
                                a.Section,
                                a.Package,
                                a.Priority,
                                a.Version,
                                a.OldVersion,
                                a.Title,
                                a.CurrentState,
                                a.Description
                            }));
        }

        if (settings.Node.Firewall.Enabled)
        {
            pt.Step("Firewall");
            var fw = settings.Node.Firewall;

            AddFirewallRules(sw, await client.Nodes[node].Firewall.Rules.GetAsync());

            AddLogs(sw,
                    "Firewall Logs",
                    await client.Nodes[node].Firewall.Log.GetAsync(limit: fw.LogMaxCount > 0
                                                                            ? fw.LogMaxCount
                                                                            : null,
                                                                   since: fw.LogSinceUnix,
                                                                   until: fw.LogUntilUnix));
        }

        if (settings.Node.IncludeSslCertificates)
        {
            pt.Step("SSL Certificates");
            sw.CreateTable("SSL Certificates",
                           (await client.Nodes[node].Certificates.Info.GetAsync()).Select(cert => new
                           {
                               cert.FileName,
                               cert.Subject,
                               cert.Issuer,
                               cert.Fingerprint,
                               cert.PublicKeyType,
                               cert.PublicKeyBits,
                               San = string.Join(", ", cert.San ?? []),
                               NotBefore = FromUnixTime(cert.NotBefore),
                               NotAfter = FromUnixTime(cert.NotAfter),
                               DaysUntilExpiry = (FromUnixTime(cert.NotAfter)!.Value - DateTime.UtcNow).Days,
                           }));
        }

        if (settings.Node.Tasks.Enabled)
        {
            pt.Step("Tasks");
            var taskSettings = settings.Node.Tasks;
            sw.CreateTable("Tasks",
                           (await client.Nodes[node].Tasks.GetAsync(
                               errors: taskSettings.OnlyErrors ? true : null,
                               limit: taskSettings.MaxCount > 0 ? taskSettings.MaxCount : null,
                               source: taskSettings.Source == "all" ? null : taskSettings.Source
                           )).Select(a => new
                           {
                               a.UniqueTaskId,
                               a.Type,
                               a.User,
                               a.Status,
                               a.StatusOk,
                               StartTime = a.StartTimeDate,
                               EndTime = a.EndTimeDate,
                               a.Duration,
                               a.VmId
                           }),
                           tbl => sw.ApplyVmIdLinks(tbl));
        }

        sw.WriteIndex();
        sw.AdjustColumns();
    }

    static List<dynamic> MakeZfsStatus(string poolName,
                                     IEnumerable<NodeDiskZfsDetail.Child> children,
                                     List<dynamic>? parentData,
                                     int level)
    {
        parentData ??= [];
        foreach (var child in children)
        {
            parentData.Add(new
            {
                PoolName = poolName,
                Name = $"{new string(' ', level * 2)}{child.Name}",
                Health = child.State,
                child.Read,
                child.Write,
                child.Checksum,
                Message = child.Msg
            });

            if (child.Children != null && child.Children.Any())
            {
                MakeZfsStatus(poolName, child.Children, parentData, level + 1);
            }
        }
        return parentData;
    }
}
