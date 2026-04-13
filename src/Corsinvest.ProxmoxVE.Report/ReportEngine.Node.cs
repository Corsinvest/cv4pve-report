/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private class NodeFetchData
    {
        public required ClusterResource Item { get; init; }
        public NodeStatus? Status { get; init; }
        public NodeVersion? Version { get; init; }
        public NodeSubscription? Subscription { get; init; }
        public NodeDns? Dns { get; init; }
        public NodeTime? Time { get; init; }
        public IEnumerable<NodeNetwork> Networks { get; init; } = [];
    }

    private async Task<NodeFetchData> FetchNodeDataAsync(ClusterResource item, ProgressTracker pt)
    {
        pt.Next(item);

        if (item.IsUnknown || !item.IsOnline) { return new NodeFetchData { Item = item }; }

        pt.Step("Status/Version/Subscription/DNS/Time/Network");
        var node = client.Nodes[item.Node];
        var statusTask = node.Status.GetAsync();
        var versionTask = node.Version.GetAsync();
        var subscriptionTask = node.Subscription.GetAsync();
        var dnsTask = node.Dns.GetAsync();
        var timeTask = node.Time.GetAsync();
        var networksTask = node.Network.GetAsync();
        await Task.WhenAll(statusTask, versionTask, subscriptionTask, dnsTask, timeTask, networksTask);

        return new()
        {
            Item = item,
            Status = statusTask.Result,
            Version = versionTask.Result,
            Subscription = subscriptionTask.Result,
            Dns = dnsTask.Result,
            Time = timeTask.Result,
            Networks = networksTask.Result,
        };
    }

    private async Task<int> AddNodesDataAsync(XLWorkbook workbook)
    {
        var sw = CreateSheetWriter(workbook, "Nodes");
        var items = new List<dynamic>();

        var filtered = GetResources(ClusterResourceType.Node)
                                 .OrderBy(a => a.Id)
                                 .ToList();

        var pt = new ProgressTracker(_progress, filtered.Count);

        var results = (await RunParallelAsync(filtered, item => FetchNodeDataAsync(item, pt)))
                            .OrderBy(a => a.Item.Id).ToList();

        foreach (var d in results)
        {
            _pendingNodeNetworkRows.AddRange(d.Networks.Select(a => (d.Item.Node, a)));

            items.Add(new
            {
                d.Item.Node,
                d.Item.Status,
                Uptime = FormatHelper.UptimeInfo(d.Item.Uptime),
                d.Item.CpuSize,
                CpuCpus = d.Status?.CpuInfo.Cpus,
                CpuSockets = d.Status?.CpuInfo.Sockets,
                CpuCores = d.Status?.CpuInfo.Cores,
                CpuModel = d.Status?.CpuInfo.Model,
                CpuMhz = d.Status?.CpuInfo.Mhz,
                CpuHvm = d.Status?.CpuInfo.Hvm,
                MemorySizeGB = ToGB(d.Item.MemorySize),
                MemoryUsageGB = ToGB(d.Item.MemoryUsage),
                MemoryUsagePct = d.Item.MemoryUsagePercentage,
                SwapTotalGB = ToGB(d.Status?.Swap.Total ?? 0),
                SwapUsedGB = ToGB(d.Status?.Swap.Used ?? 0),

                SwapUsagePct = d.Status?.Swap.Total > 0
                                ? (double)(d.Status.Swap.Used) / d.Status.Swap.Total
                                : (double?)null,

                DiskSizeGB = ToGB(d.Item.DiskSize),
                DiskUsageGB = ToGB(d.Item.DiskUsage),
                DiskUsagePct = d.Item.DiskUsagePercentage,
                RootFsTotalGB = ToGB(d.Status?.RootFs.Total ?? 0),
                RootFsUsedGB = ToGB(d.Status?.RootFs.Used ?? 0),

                RootFsUsagePct = d.Status?.RootFs.Total > 0
                                    ? (double)d.Status.RootFs.Used / d.Status.RootFs.Total
                                    : (double?)null,

                KernelVersion = d.Status?.Kversion,
                KernelRelease = d.Status?.CurrentKernel?.Release,
                BootMode = d.Status?.BootInfo?.Mode,
                SecurebootFlag = ToX(d.Status?.BootInfo?.Secureboot),
                d.Item.CgroupMode,
                d.Status?.PveVersion,
                VersionVersion = d.Version?.Version,
                VersionRelease = d.Version?.Release,
                Subscription = d.Item.NodeLevel,
                SubscriptionProductName = d.Subscription?.ProductName,
                SubscriptionRegDate = d.Subscription?.RegDate,
                Time = d.Time?.Timezone,
                DnsSearch = d.Dns?.Search,
                d.Dns?.Dns1,
                d.Dns?.Dns2,
                d.Dns?.Dns3,
            });

            if (!d.Item.IsUnknown && d.Item.IsOnline && settings.Node.Detail.Enabled)
            {
                await AddNodeDetailAsync(workbook, d, pt);
            }
        }

        sw.CreateTable(null,
                       items,
                       tbl => sw.ApplyColumnLinks(tbl, "Node", cell => $"node:{cell.Value}"));

        sw.AdjustColumns();

        return filtered.Count;
    }

    private async Task AddNodeDetailAsync(XLWorkbook workbook, NodeFetchData data, ProgressTracker pt)
    {
        var node = data.Item.Node;
        var sw = CreateSheetWriter(workbook, GetSheetName(ClusterResourceType.Node, node)!);
        sw.WriteBackLink("Nodes", "list:nodes");

        sw.WriteKeyValue(node,
                         new()
                         {
                             ["Status"] = data.Item.Status,
                             ["CPU Sockets"] = data.Status!.CpuInfo.Sockets,
                             ["CPU Cores"] = data.Status!.CpuInfo.Cores,
                             ["CPU Model"] = data.Status!.CpuInfo.Model,
                             ["CPU MHz"] = data.Status!.CpuInfo.Mhz,
                             ["Memory GB"] = ToGB(data.Status!.Memory.Total),
                             ["Memory Used GB"] = ToGB(data.Status!.Memory.Used),
                             ["Swap GB"] = ToGB(data.Status!.Swap.Total),
                             ["Root FS GB"] = ToGB(data.Status!.RootFs.Total),
                             ["Kernel"] = data.Status!.Kversion,
                             ["Kernel Release"] = data.Status!.CurrentKernel?.Release,
                             ["Boot Mode"] = data.Status!.BootInfo?.Mode,
                             ["Secure Boot"] = data.Status!.BootInfo?.Secureboot,
                             ["CPU HVM"] = data.Status!.CpuInfo.Hvm,
                             ["Uptime"] = FormatHelper.UptimeInfo(data.Item.Uptime),
                             ["PVE Version"] = data.Status!.PveVersion,
                             ["Version"] = $"{data.Version!.Version}-{data.Version!.Release}",
                             ["Subscription"] = data.Subscription!.ProductName,
                             ["Subscription Expiry"] = data.Subscription!.RegDate,
                             ["Timezone"] = data.Time!.Timezone,
                             ["DNS Search"] = data.Dns!.Search,
                             ["DNS 1"] = data.Dns!.Dns1,
                             ["DNS 2"] = data.Dns!.Dns2,
                             ["DNS 3"] = data.Dns!.Dns3,
                         });

        var tableCount = 1  // Services
                       + 1  // Network
                       + (settings.Node.Detail.Disk.IncludeDiskDetail ? 1 : 0)   // Disks
                       + (settings.Node.Detail.Disk.IncludeSmartData ? 1 : 0)
                       + (settings.Node.Detail.Disk.IncludeDiskDetail ? 2 : 0)   // ZFS Pools + ZFS Pool Status
                       + (settings.Node.Detail.Disk.IncludeDiskDetail ? 1 : 0)   // Directory
                       + (settings.Node.Detail.IncludeApt ? 3 : 0)               // Repositories + Updates + Versions
                       + (settings.Firewall.Enabled && settings.Node.Detail.IncludeFirewallLog ? 1 : 0)  // Firewall Logs
                       + 1  // SSL Certificates
                       + (settings.Node.Detail.Tasks.Enabled ? 1 : 0)
                       + 1;                                                        // /etc/hosts

        sw.ReserveIndexRows(tableCount);

        pt.Step("Services/SSL Certificates/Hosts");
        var servicesTask = client.Nodes[node].Services.GetAsync();
        var certificatesTask = client.Nodes[node].Certificates.Info.GetAsync();
        var hostsTask = client.Nodes[node].Hosts.GetEtcHosts();

        await Task.WhenAll(servicesTask, certificatesTask, hostsTask);

        sw.CreateTable("Services",
                       servicesTask.Result.Select(a => new
                       {
                           a.Name,
                           a.Service,
                           a.State,
                           a.ActiveState,
                           a.UnitState,
                           DescriptionWrap = a.Description,
                       }));

        sw.CreateTable("Network",
                       data.Networks.Select(a => new
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

        sw.CreateTable("/etc/hosts",
                       ((string)hostsTask.Result.ToData().data)
                                .Split('\n')
                                .Select(a => a.Trim())
                                .Where(a => a.Length > 0 && !a.StartsWith('#'))
                                .Select(a =>
                                {
                                    var parts = a.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                                    return new
                                    {
                                        IP = parts[0],
                                        HostnamesWrap = parts.Skip(1).JoinAsString(Environment.NewLine)
                                    };
                                })
                                .ToList());

        if (settings.Node.Detail.Disk.IncludeDiskDetail || settings.Node.Detail.Disk.IncludeSmartData)
        {
            var disksData = await client.Nodes[node].Disks.List.GetAsync(include_partitions: true);

            if (settings.Node.Detail.Disk.IncludeDiskDetail)
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

            if (settings.Node.Detail.Disk.IncludeSmartData)
            {
                pt.Step("S.M.A.R.T. Data");
                var rootDisks = disksData.Where(a => string.IsNullOrEmpty(a.Parent)).ToList();
                var smartResults = await RunParallelAsync(rootDisks, d => client.GetDiskSmart(node, d.DevPath));

                sw.CreateTable("S.M.A.R.T. Data",
                               rootDisks.Zip(smartResults, (disk, smart) => (smart.Attributes ?? []).Select(attr => new
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
                               }))
                               .SelectMany(x => x)
                               .ToList());
            }

            if (settings.Node.Detail.Disk.IncludeDiskDetail)
            {
                pt.Step("Directory/ZFS Pools");
                var directoryTask = client.Nodes[node].Disks.Directory.GetAsync();
                var zfsPoolsListTask = client.Nodes[node].Disks.Zfs.GetAsync();
                await Task.WhenAll(directoryTask, zfsPoolsListTask);

                sw.CreateTable("Directory",
                               directoryTask.Result.Select(a => new
                               {
                                   a.Device,
                                   a.Path,
                                   a.Type,
                                   a.Options,
                                   a.UnitFile
                               }));

                var zfsPoolList = zfsPoolsListTask.Result.ToList();
                var zfsPoolDetails = await RunParallelAsync(zfsPoolList, p => client.Nodes[node].Disks.Zfs[p.Name].GetAsync());

                sw.CreateTable("ZFS Pools", zfsPoolList.Zip(zfsPoolDetails, (pool, poolData) => new
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
                })
                .ToList());

                sw.CreateTable("ZFS Pool Status",
                               zfsPoolList.Zip(zfsPoolDetails, (pool, poolData) => MakeZfsStatus(pool.Name, poolData.Children, null, 0))
                                          .SelectMany(x => x)
                                          .ToList());
            }
        }

        if (settings.Node.Detail.IncludeApt)
        {
            pt.Step("Apt Repository/Updates/Versions");
            var aptRepositoriesTask = client.Nodes[node].Apt.Repositories.GetAsync();
            var aptUpdatesTask = client.Nodes[node].Apt.Update.GetAsync();
            var aptVersionsTask = client.Nodes[node].Apt.Versions.GetAsync();
            await Task.WhenAll(aptRepositoriesTask, aptUpdatesTask, aptVersionsTask);

            sw.CreateTable("Apt Repository",
                           aptRepositoriesTask.Result.Files.SelectMany(a => a.Repositories, (file, repo) => new
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

            sw.CreateTable("Apt Update",
                           aptUpdatesTask.Result.Select(a => new
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

            sw.CreateTable("Package Version",
                           aptVersionsTask.Result.Select(a => new
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

        if (settings.Firewall.Enabled && settings.Node.Detail.IncludeFirewallLog)
        {
            pt.Step("Firewall Logs");
            AddLogs(sw,
                    "Firewall Logs",
                    await client.Nodes[node].Firewall.Log.GetAsync(limit: settings.Firewall.Limit,
                                                                   since: settings.Firewall.SinceUnix,
                                                                   until: settings.Firewall.UntilUnix));
        }

        sw.CreateTable("SSL Certificates",
                       certificatesTask.Result.Select(cert => new
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
                           DaysUntilExpiry = FromUnixTime(cert.NotAfter) is { } expiry
                                                ? (expiry - DateTime.UtcNow).Days
                                                : (int?)null,
                       }));

        if (settings.Node.Detail.Tasks.Enabled)
        {
            pt.Step("Tasks");
            var taskSettings = settings.Node.Detail.Tasks;
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

            if (child.Children?.Any() is true)
            {
                MakeZfsStatus(poolName, child.Children, parentData, level + 1);
            }
        }
        return parentData;
    }
}
