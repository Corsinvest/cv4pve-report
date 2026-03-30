/*
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
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
        var sw = new SheetWriter(workbook.Worksheets.Add("Nodes"), _sheetLinks);
        var resources = await client.GetResourcesAsync(ClusterResourceType.Node);
        var items = new List<dynamic>();

        var filtered = resources.Where(a => CheckNames(settings.Node.Names, a.Node))
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
        var sheetName = GetSheetName(ClusterResourceType.Node, node)!;
        var sw = new SheetWriter(workbook.Worksheets.Add(sheetName), _sheetLinks);

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

        var tableCount = (settings.Node.IncludeServices ? 1 : 0)
                       + (settings.Node.IncludeNetwork ? 1 : 0)
                       + (settings.Node.IncludeDisks ? 1 : 0)
                       + (settings.Node.IncludeDisks && settings.Node.IncludeSmartData ? 1 : 0)
                       + (settings.Node.IncludeReplication ? 1 : 0)
                       + (settings.Node.RrdData.Enabled ? 1 : 0)
                       + (settings.Node.IncludeAptUpdates ? 1 : 0)
                       + (settings.Node.IncludeAptVersions ? 1 : 0)
                       + (settings.Node.Firewall.Enabled ? 2 : 0)
                       + (settings.Node.IncludeSslCertificates ? 1 : 0)
                       + (settings.Node.Tasks.Enabled ? 1 : 0)
                       + (settings.Node.Syslog.Enabled ? 1 : 0);

        sw.ReserveIndexRows(tableCount);

        if (settings.Node.IncludeServices)
        {
            pt.Step("Services");
            sw.CreateTable("Services",
                           (await client.Nodes[node].Services.GetAsync())
                           .Select(a => new
                           {
                               a.Name,
                               a.State,
                               a.Service,
                               a.Description
                           }));
        }

        if (settings.Node.IncludeNetwork)
        {
            pt.Step("Network");
            sw.CreateTable("Network",
                           (await client.Nodes[node].Network.GetAsync())
                           .Select(a => new
                           {
                               a.Active,
                               a.Type,
                               a.Interface,
                               a.Method,
                               a.Method6,
                               a.Gateway,
                               a.Priority,
                               a.BondMode,
                               a.Address,
                               a.Netmask,
                               a.Cidr,
                               a.Comments,
                               a.BondMiimon,
                               a.Slaves,
                               a.AutoStart,
                               a.BridgeStp,
                               a.BridgeVlanAware,
                               a.BridgeVids,
                               a.BridgeFd,
                               a.BridgePorts
                           }),
                           tbl => sw.RegisterNetworkLinks(tbl, node));
        }

        if (settings.Node.IncludeDisks)
        {
            pt.Step("Disks");
            var disksData = await client.Nodes[node].Disks.List.GetAsync();
            sw.CreateTable("Disks",
                           disksData.Select(a => new
                           {
                               a.Used,
                               DevicePath = a.DevPath,
                               a.Vendor,
                               a.Serial,
                               a.Type,
                               a.Model,
                               a.Wwn,
                               a.Health,
                               a.Gpt,
                               a.Wearout,
                               a.Rpm,
                               SizeGB = ToGB(a.Size)
                           }));

            if (settings.Node.IncludeSmartData)
            {
                pt.Step("Smart");
                var smartItems = new List<dynamic>();
                foreach (var disk in disksData)
                {
                    var smart = await client.GetDiskSmart(node, disk.DevPath);
                    foreach (var attr in smart.Attributes ?? [])
                    {
                        smartItems.Add(new
                        {
                            Disk = disk.DevPath,
                            disk.Model,
                            attr.Id,
                            attr.Name,
                            attr.Value,
                            attr.Worst,
                            attr.Threshold,
                            attr.Flags,
                            attr.Raw
                        });
                    }
                }
                sw.CreateTable("SMART Data", smartItems);
            }
        }

        if (settings.Node.IncludeReplication)
        {
            pt.Step("Replication");
            sw.CreateTable("Replication",
                           (await client.Nodes[node].Replication.GetAsync())
                            .Select(a => new
                            {
                                a.Disable,
                                a.Id,
                                a.Type,
                                a.Guest,
                                a.Source,
                                a.Target,
                                a.Schedule,
                                a.FailCount,
                                a.Duration,
                                LastSync = DateTimeOffset.FromUnixTimeSeconds(a.LastSync).DateTime,
                                NextSync = DateTimeOffset.FromUnixTimeSeconds(a.NextSync).DateTime,
                                a.Error
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

        if (settings.Node.IncludeAptUpdates)
        {
            pt.Step("Apt Updates");
            sw.CreateTable("Apt Update",
                           (await client.Nodes[node].Apt.Update.GetAsync()).Select(a => new
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
                           (await client.Nodes[node].Apt.Versions.GetAsync()).Select(a => new
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
            AddLogs(sw, "Firewall Logs", await client.Nodes[node].Firewall.Log.GetAsync(
                limit: fw.LogMaxCount > 0 ? fw.LogMaxCount : null,
                since: fw.LogSince.HasValue ? (int)new DateTimeOffset(fw.LogSince.Value.ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds() : null,
                until: fw.LogUntil.HasValue ? (int)new DateTimeOffset(fw.LogUntil.Value.ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds() : null
            ));
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
                               NotBefore = DateTimeOffset.FromUnixTimeSeconds(cert.NotBefore).DateTime,
                               NotAfter = DateTimeOffset.FromUnixTimeSeconds(cert.NotAfter).DateTime,
                               DaysUntilExpiry = (DateTimeOffset.FromUnixTimeSeconds(cert.NotAfter).DateTime - DateTime.UtcNow).Days,
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

        if (settings.Node.Syslog.Enabled)
        {
            pt.Step("Syslog");
            var s = settings.Node.Syslog;
            var logs = (await client.Nodes[node].Syslog.Syslog(limit: s.MaxCount > 0 ? s.MaxCount : null,
                                                               service: string.IsNullOrWhiteSpace(s.Service) ? null : s.Service,
                                                               since: s.Since.HasValue ? s.Since.Value.ToString("yyyy-MM-dd") : null,
                                                               until: s.Until.HasValue ? s.Until.Value.ToString("yyyy-MM-dd") : null))
                                                       .ToLogs();
            sw.CreateTable("Syslog", logs.Select(a => new { Log = a }));
        }

        sw.WriteIndex();
        sw.AdjustColumns();
    }
}
