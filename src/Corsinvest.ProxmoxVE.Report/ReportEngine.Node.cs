/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task AddNodesDataAsync(XLWorkbook workbook)
    {
        var sw = CreateSheetWriter(workbook, "Nodes");
        var items = new List<dynamic>();

        var filtered = GetResources(ClusterResourceType.Node)
                                 .OrderBy(a => a.Id)
                                 .ToList();

        var pt = new ProgressTracker(_progress, filtered.Count);

        foreach (var item in filtered)
        {
            pt.Next(item);

            pt.Step("Status");
            var status = item.IsUnknown
                            ? null
                            : await client.Nodes[item.Node].Status.GetAsync();

            pt.Step("Version");
            var version = item.IsUnknown
                            ? null
                            : await client.Nodes[item.Node].Version.GetAsync();

            pt.Step("Subscription");
            var subscription = item.IsUnknown
                                ? null
                                : await client.Nodes[item.Node].Subscription.GetAsync();

            pt.Step("DNS");
            var dns = item.IsUnknown
                        ? null
                        : await client.Nodes[item.Node].Dns.GetAsync();

            pt.Step("Time");
            var time = item.IsUnknown
                        ? null
                        : await client.Nodes[item.Node].Time.GetAsync();

            items.Add(new
            {
                item.Node,
                item.Status,
                Uptime = FormatHelper.UptimeInfo(item.Uptime),
                item.CpuSize,
                CpuCpus = status?.CpuInfo.Cpus,
                CpuSockets = status?.CpuInfo.Sockets,
                CpuCores = status?.CpuInfo.Cores,
                CpuModel = status?.CpuInfo.Model,
                CpuMhz = status?.CpuInfo.Mhz,
                CpuHvm = status?.CpuInfo.Hvm,
                MemorySizeGB = ToGB(item.MemorySize),
                MemoryUsageGB = ToGB(item.MemoryUsage),
                MemoryUsagePct = item.MemoryUsagePercentage,
                SwapTotalGB = ToGB(status?.Swap.Total ?? 0),
                SwapUsedGB = ToGB(status?.Swap.Used ?? 0),
                SwapUsagePct = status?.Swap.Total > 0 ? (double)(status.Swap.Used) / status.Swap.Total : (double?)null,
                DiskSizeGB = ToGB(item.DiskSize),
                DiskUsageGB = ToGB(item.DiskUsage),
                DiskUsagePct = item.DiskUsagePercentage,
                RootFsTotalGB = ToGB(status?.RootFs.Total ?? 0),
                RootFsUsedGB = ToGB(status?.RootFs.Used ?? 0),
                RootFsUsagePct = status?.RootFs.Total > 0 ? (double)status.RootFs.Used / status.RootFs.Total : (double?)null,
                KernelVersion = status?.Kversion,
                KernelRelease = status?.CurrentKernel?.Release,
                BootMode = status?.BootInfo?.Mode,
                SecurebootFlag = ToX(status?.BootInfo?.Secureboot),
                item.CgroupMode,
                status?.PveVersion,
                VersionVersion = version?.Version,
                VersionRelease = version?.Release,
                Subscription = item.NodeLevel,
                SubscriptionProductName = subscription?.ProductName,
                SubscriptionRegDate = subscription?.RegDate,
                time?.Timezone,
                DnsSearch = dns?.Search,
                dns?.Dns1,
                dns?.Dns2,
                dns?.Dns3,
            });

            if (!item.IsUnknown)
            {
                await AddNodeDetailAsync(workbook,
                                         item,
                                         status!,
                                         version!,
                                         subscription!,
                                         dns!,
                                         time!,
                                         pt);
            }
        }

        sw.CreateTable(null,
                       items,
                       tbl => sw.ApplyColumnLinks(tbl, "Node", cell => $"node:{cell.Value}"));

        sw.AdjustColumns();
    }

    private async Task AddNodeDetailAsync(XLWorkbook workbook,
                                          ClusterResource item,
                                          NodeStatus status,
                                          NodeVersion version,
                                          NodeSubscription subscription,
                                          NodeDns dns,
                                          NodeTime time,
                                          ProgressTracker pt)
    {
        var node = item.Node;
        var sw = CreateSheetWriter(workbook, GetSheetName(ClusterResourceType.Node, node)!);
        sw.WriteBackLink("Nodes", "list:nodes");

        sw.WriteKeyValue(node,
                         new()
                         {
                             ["Status"] = item.Status,
                             ["CPU Sockets"] = status.CpuInfo.Sockets,
                             ["CPU Cores"] = status.CpuInfo.Cores,
                             ["CPU Model"] = status.CpuInfo.Model,
                             ["CPU MHz"] = status.CpuInfo.Mhz,
                             ["Memory GB"] = ToGB(status.Memory.Total),
                             ["Memory Used GB"] = ToGB(status.Memory.Used),
                             ["Swap GB"] = ToGB(status.Swap.Total),
                             ["Root FS GB"] = ToGB(status.RootFs.Total),
                             ["Kernel"] = status.Kversion,
                             ["Kernel Release"] = status.CurrentKernel?.Release,
                             ["Boot Mode"] = status.BootInfo?.Mode,
                             ["Secure Boot"] = status.BootInfo?.Secureboot,
                             ["CPU HVM"] = status.CpuInfo.Hvm,
                             ["Uptime"] = FormatHelper.UptimeInfo(item.Uptime),
                             ["PVE Version"] = status.PveVersion,
                             ["Version"] = $"{version.Version}-{version.Release}",
                             ["Subscription"] = subscription.ProductName,
                             ["Subscription Expiry"] = subscription.RegDate,
                             ["Timezone"] = time.Timezone,
                             ["DNS Search"] = dns.Search,
                             ["DNS 1"] = dns.Dns1,
                             ["DNS 2"] = dns.Dns2,
                             ["DNS 3"] = dns.Dns3,
                         });

        var tableCount = 1  // Services
                       + 1  // Network
                       + (settings.Node.Disk.IncludeDiskDetail ? 1 : 0)   // Disks
                       + (settings.Node.Disk.IncludeSmartData ? 1 : 0)
                       + (settings.Node.Disk.IncludeDiskDetail ? 2 : 0)   // ZFS Pools + ZFS Pool Status
                       + (settings.Node.Disk.IncludeDiskDetail ? 1 : 0)   // Directory
                       + (settings.Node.IncludeApt ? 3 : 0)               // Repositories + Updates + Versions
                       + (settings.Firewall.Enabled ? 1 : 0)  // Firewall Logs
                       + 1  // SSL Certificates
                       + (settings.Node.Tasks.Enabled ? 1 : 0);

        sw.ReserveIndexRows(tableCount);

        pt.Step("Services");
        sw.CreateTable("Services",
                       (await client.Nodes[node].Services.GetAsync())
                       .Select(a => new
                       {
                           a.Name,
                           a.Service,
                           a.State,
                           a.ActiveState,
                           a.UnitState,
                           DescriptionWrap = a.Description,
                       }));

        pt.Step("Network");
        var nodeNets = await client.Nodes[node].Network.GetAsync();
        AppendNodeNetworkRows(workbook, node, nodeNets);
        sw.CreateTable("Network",
                       nodeNets.Select(a => new
                       {
                           ActiveFlag = ToX(a.Active),
                           AutoStartFlag = ToX(a.AutoStart),
                           ExistsFlag = ToX(a.Exists),
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
                           CommentsWrap = a.Comments,
                           a.Comments6,
                       }),
                       tbl => sw.RegisterNetworkLinks(tbl, node));

        if (settings.Node.Disk.IncludeDiskDetail || settings.Node.Disk.IncludeSmartData)
        {
            var disksData = await client.Nodes[node].Disks.List.GetAsync(include_partitions: true);

            if (settings.Node.Disk.IncludeDiskDetail)
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
                                            MountedFlag = ToX(a.Mounted),
                                            a.ByIdLink,
                                            a.OsdId,
                                        }));
            }

            if (settings.Node.Disk.IncludeSmartData)
            {
                pt.Step("S.M.A.R.T. Data");
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
                sw.CreateTable("S.M.A.R.T. Data", smartItems);
            }

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
                    FragmentationPct = pool.Frag / 100.0,
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

        if (settings.Node.IncludeApt)
        {
            pt.Step("Apt Repository");
            var aptRepositories = await client.Nodes[node].Apt.Repositories.GetAsync();
            sw.CreateTable("Apt Repository",
                           aptRepositories.Files.SelectMany(a => a.Repositories,
                             (file, repo) => new
                             {
                                 FilePath = file.Path,
                                 file.FileType,
                                 EnabledFlag = ToX(repo.Enabled),
                                 TypesWrap = repo.Types.JoinAsString(Environment.NewLine),
                                 URIsWrap = repo.URIs.JoinAsString(Environment.NewLine),
                                 SuitesWrap = repo.Suites.JoinAsString(Environment.NewLine),
                                 ComponentsWrap = repo.Components.JoinAsString(Environment.NewLine),
                                 CommentWrap = repo.Comment,
                             }));

            pt.Step("Apt Updates");
            sw.CreateTable("Apt Update",
                           (await client.Nodes[node].Apt.Update.GetAsync())
                            .Select(a => new
                            {
                                a.Package,
                                a.Version,
                                a.OldVersion,
                                a.Arch,
                                a.Origin,
                                a.Section,
                                a.Priority,
                                a.Title,
                                a.Description
                            }));

            pt.Step("Apt Versions");
            sw.CreateTable("Package Version",
                           (await client.Nodes[node].Apt.Versions.GetAsync())
                            .Select(a => new
                            {
                                a.Package,
                                a.Version,
                                a.OldVersion,
                                a.CurrentState,
                                a.Arch,
                                a.Origin,
                                a.Section,
                                a.Priority,
                                a.Title,
                                a.Description
                            }));
        }

        if (settings.Firewall.Enabled)
        {
            pt.Step("Firewall Logs");
            AddLogs(sw,
                    "Firewall Logs",
                    await client.Nodes[node].Firewall.Log.GetAsync(limit: settings.Firewall.Limit,
                                                                   since: settings.Firewall.SinceUnix,
                                                                   until: settings.Firewall.UntilUnix));
        }

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
                               a.VmId,
                               a.User,
                               a.Status,
                               a.StatusOk,
                               StartTime = a.StartTimeDate,
                               EndTime = a.EndTimeDate,
                               a.Duration,
                           }),
                           tbl => sw.ApplyVmIdLinks(tbl));
        }

        sw.WriteIndex();
        sw.AdjustColumns();
    }

    private static List<dynamic> MakeZfsStatus(string poolName,
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
