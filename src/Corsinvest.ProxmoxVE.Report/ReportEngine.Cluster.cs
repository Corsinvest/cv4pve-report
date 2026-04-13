/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddClusterDataAsync(XLWorkbook workbook)
    {
        if (!settings.Cluster.IncludeSheet) { return 0; }

        var sw = CreateSheetWriter(workbook, "Cluster");

        var pveVersion = await client.Version.GetAsync();
        var pveMajor = int.TryParse(pveVersion.Version?.Split('.')[0], out var v)
                        ? v
                        : 0;

        var haGroupsSupported = pveMajor < 9;

        var tableCount = 1  // Status
                       + 1  // Options
                       + 7  // Security
                       + (settings.Firewall.Enabled ? 1 : 0)  // Firewall Options
                       + 1  // Backup Jobs
                       + 1  // Replication
                       + 1  // Storages
                       + 1  // Metric Servers
                       + 5  // SDN
                       + 3  // Mapping
                       + 1  // Pools
                       + (haGroupsSupported ? 3 : 2)  // HA
                       ;

        var allClusterStatus = await client.Cluster.Status.GetAsync();
        var clusterStatus = allClusterStatus.FirstOrDefault(a => a.Type == "cluster");

        sw.WriteKeyValue("Cluster", new()
        {
            ["Name"] = clusterStatus?.Name ?? "-",
            ["Nodes"] = clusterStatus?.Nodes ?? 0,
            ["Version"] = clusterStatus?.Version ?? 0,
            ["Quorate"] = clusterStatus?.Quorate == 1 ? "Yes" : "No",
        });

        sw.ReserveIndexRows(tableCount);

        ReportGlobal("Cluster: Status");
        sw.CreateTable("Status",
                       allClusterStatus.Select(a => new
                       {
                           a.Id,
                           a.Name,
                           IsOnlineFlag = ToX(a.IsOnline),
                           a.Type,
                           a.Nodes,
                           a.Version,
                           a.Quorate,
                           Level = NodeHelper.DecodeLevelSupport(a.Level),
                           a.IpAddress,
                           a.NodeId,
                       }));

        // Fetch all independent data in parallel
        ReportGlobal("Cluster: Fetching data");

        var optionsTask = client.Cluster.Options.GetAsync();
        var usersTask = client.Access.Users.GetAsync(full: true);
        var tfaTask = client.Access.Tfa.GetAsync();
        var groupsTask = client.Access.Groups.GetAsync();
        var rolesTask = client.Access.Roles.GetAsync();
        var aclTask = client.Access.Acl.GetAsync();
        var domainsTask = client.Access.Domains.GetAsync();
        var backupJobsTask = client.Cluster.Backup.GetAsync();
        var replicationTask = client.Cluster.Replication.GetAsync();
        var storagesTask = client.Storage.GetAsync();
        var metricServersTask = client.Cluster.Metrics.Server.GetAsync();
        var sdnZonesTask = client.Cluster.Sdn.Zones.GetAsync();
        var vnetsTask = client.Cluster.Sdn.Vnets.GetAsync();
        var sdnControllersTask = client.Cluster.Sdn.Controllers.GetAsync();
        var sdnIpamsTask = client.Cluster.Sdn.Ipams.GetAsync();
        var mappingDirTask = client.Cluster.Mapping.Dir.GetAsync();
        var mappingPciTask = client.Cluster.Mapping.Pci.GetAsync();
        var mappingUsbTask = client.Cluster.Mapping.Usb.GetAsync();
        var poolsTask = client.Pools.GetAsync();
        var haResourcesTask = client.Cluster.Ha.Resources.GetAsync();
        var haStatusTask = client.Cluster.Ha.Status.Current.GetAsync();

        var fwOptionsTask = settings.Firewall.Enabled
                                ? client.Cluster.Firewall.Options.GetAsync()
                                : null;

        var haGroupsTask = haGroupsSupported
                            ? client.Cluster.Ha.Groups.GetAsync()
                            : null;

        var waitTasks = new List<Task>
        {
            optionsTask, usersTask, tfaTask, groupsTask, rolesTask, aclTask, domainsTask,
            backupJobsTask, replicationTask, storagesTask, metricServersTask,
            sdnZonesTask, vnetsTask, sdnControllersTask, sdnIpamsTask,
            mappingDirTask, mappingPciTask, mappingUsbTask,
            poolsTask, haResourcesTask, haStatusTask,
        };
        if (fwOptionsTask != null) { waitTasks.Add(fwOptionsTask); }
        if (haGroupsTask != null) { waitTasks.Add(haGroupsTask); }

        await Task.WhenAll(waitTasks);

        // --- Write tables ---
        ReportGlobal("Cluster: Options");
        sw.CreateTable("Options",
                       [new
                           {
                               optionsTask.Result.Console,
                               optionsTask.Result.Keyboard,
                               optionsTask.Result.MacPrefix,
                               DescriptionWrap = optionsTask.Result.Description,
                               AllowedTags = ToNewLine(string.Join(",", optionsTask.Result.AllowedTags ?? [])),
                               MigrationType = optionsTask.Result.Migration?.Type,
                               MigrationNetwork = optionsTask.Result.Migration?.Network,
                           }]);

        ReportGlobal("Cluster: Security");
        sw.CreateTable("Users",
                       usersTask.Result.Select(a => new
                       {
                           a.Id,
                           EnableFlag = ToX(a.Enable),
                           a.Firstname,
                           a.Lastname,
                           a.Email,
                           a.Groups,
                           a.Keys,
                           TotpLockedFlag = ToX(a.TotpLocked),
                           TfaLockedUntil = FromUnixTime(a.TfaLockedUntil ?? 0),
                           Expire = FromUnixTime(a.Expire),
                           CommentWrap = a.Comment,
                       }));

        sw.CreateTable("API Tokens",
                       usersTask.Result.SelectMany(a => a.Tokens.Select(t => new
                       {
                           User = a.Id,
                           TokenId = t.Id,
                           Expire = FromUnixTime(t.Expire),
                           PrivSeparatedFlag = ToX(t.Privsep == 1),
                           CommentWrap = t.Comment
                       })));

        sw.CreateTable("Two-Factor Authentication",
                       tfaTask.Result.Select(t => new
                       {
                           User = t.UserId,
                           TfaTypes = string.Join(", ", t.Entries?.Select(e => e.Type).Distinct() ?? []),
                           TfaCount = t.Entries?.Count() ?? 0
                       }));

        sw.CreateTable("Groups",
                       groupsTask.Result.Select(a => new
                       {
                           a.Id,
                           a.Users,
                           a.Comment
                       }));

        sw.CreateTable("Roles",
                       rolesTask.Result.Select(a => new
                       {
                           a.Id,
                           Privileges = ToNewLine(a.Privileges),
                           SpecialFlag = ToX(a.Special == 1)
                       }));

        sw.CreateTable("ACL",
                       aclTask.Result.Select(a => new
                       {
                           a.Path,
                           UsersOrGroup = a.UsersGroupid,
                           a.Type,
                           Id = a.Roleid,
                           PropagateFlag = ToX(a.Propagate == 1),
                       }));

        sw.CreateTable("Domains",
                       domainsTask.Result.Select(a => new
                       {
                           a.Realm,
                           a.Type,
                           a.Tfa,
                           a.Comment
                       }));

        if (settings.Firewall.Enabled && fwOptionsTask != null)
        {
            ReportGlobal("Cluster: Firewall Options");
            var fwOptions = fwOptionsTask.Result;
            sw.CreateTable("Firewall Options",
                           [new
                           {
                                fwOptions.Enable,
                                fwOptions.PolicyIn,
                                fwOptions.PolicyOut,
                                fwOptions.LogRatelimit
                            }]);
        }

        ReportGlobal("Cluster: Backup Jobs");
        sw.CreateTable("Backup Jobs",
                       backupJobsTask.Result.Select(a => new
                       {
                           a.Id,
                           a.Enabled,
                           a.All,
                           VmId = ToNewLine(a.VmId),
                           a.Mode,
                           a.Storage,
                           a.StartTime,
                           a.Schedule,
                           a.DayOfWeek,
                           a.Compress,
                           a.Type,
                           a.Mailto,
                           a.MailNotification,
                           a.NotesTemplate,
                           a.Pool,
                           a.Node,
                           a.Quiet,
                           NextRun = FromUnixTime(a.NextRun),
                       }));

        ReportGlobal("Cluster: Replication");
        sw.CreateTable("Replication",
                       replicationTask.Result.Select(a => new
                       {
                           a.Id,
                           a.Type,
                           a.Guest,
                           a.Source,
                           a.Target,
                           a.Schedule,
                           DisableFlag = ToX(a.Disable),
                           a.Rate,
                           a.Comment,
                           a.JobNum,
                           a.RemoveJob,
                       }),
                       tbl => sw.ApplyReplicationLinks(tbl));

        ReportGlobal("Cluster: Storages");
        sw.CreateTable("Storages",
                       storagesTask.Result.Select(a => new
                       {
                           a.Storage,
                           a.Type,
                           ContentWrap = ToNewLine(a.Content),
                           SharedFlag = ToX(a.Shared),
                           DisableFlag = ToX(a.Disable),
                           a.Nodes,
                           a.Path,
                           a.Mountpoint,
                           a.Server,
                           a.Export,
                           a.Datastore,
                           a.Pool,
                           a.Username,
                           a.Monhost,
                           a.Sparse,
                           a.Krbd,
                           a.Preallocation,
                           a.PruneBackups,
                       }));

        ReportGlobal("Cluster: Metric Servers");
        sw.CreateTable("Metric Servers",
                       metricServersTask.Result.Select(a => new
                       {
                           a.Id,
                           a.Server,
                           a.Port,
                           a.Type,
                           DisableFlag = ToX(a.Disable)
                       }));

        ReportGlobal("Cluster: SDN");
        sw.CreateTable("SDN Zones",
                       sdnZonesTask.Result.Select(a => new
                       {
                           a.Zone,
                           a.Type,
                           a.Mtu,
                           a.Nodes,
                           a.Bridge,
                           a.Controller,
                           a.Ipam,
                           a.Dns,
                           a.State,
                       }));

        sw.CreateTable("SDN Vnets",
                       vnetsTask.Result.Select(a => new
                       {
                           a.Vnet,
                           a.Zone,
                           a.Type,
                           a.Tag,
                           a.Alias,
                           a.VlanAware,
                           a.State
                       }));

        sw.CreateTable("SDN Controllers",
                       sdnControllersTask.Result.Select(a => new
                       {
                           a.Controller,
                           a.Type,
                           a.Asn,
                           a.Peers,
                           a.Node,
                           a.State
                       }));

        sw.CreateTable("SDN Ipams",
                       sdnIpamsTask.Result.Select(a => new
                       {
                           a.Ipam,
                           a.Type,
                       }));

        var subnetResults = await RunParallelAsync(vnetsTask.Result,
                                                   async vnet => (vnet,
                                                                  subs: await client.Cluster.Sdn.Vnets[vnet.Vnet].Subnets.GetAsync()));
        sw.CreateTable("SDN Subnets",
                       subnetResults.SelectMany(r => r.subs.Select(subnet => new
                       {
                           r.vnet.Vnet,
                           subnet.Subnet,
                           subnet.Type,
                           subnet.Gateway,
                           subnet.Snat,
                           subnet.DhcpDnsServer,
                           subnet.DnsZonePrefix,
                       })).ToList());

        ReportGlobal("Cluster: Mapping");
        sw.CreateTable("Mapping Dir",
                       mappingDirTask.Result.Select(a => new
                       {
                           a.Id,
                           DescriptionWrap = a.Description,
                           MapWrap = a.Map.JoinAsString(Environment.NewLine)
                       }));

        sw.CreateTable("Mapping PCI",
                       mappingPciTask.Result.Select(a => new
                       {
                           a.Id,
                           DescriptionWrap = a.Description,
                           MapWrap = a.Map.JoinAsString(Environment.NewLine)
                       }));

        sw.CreateTable("Mapping USB",
                       mappingUsbTask.Result.Select(a => new
                       {
                           a.Id,
                           DescriptionWrap = a.Description,
                           MapWrap = a.Map.JoinAsString(Environment.NewLine)
                       }));

        ReportGlobal("Cluster: Pools");
        var poolResults = await RunParallelAsync(poolsTask.Result,
                                                 async pool => { var detail = await client.Pools[pool.Id].GetAsync(); return (pool, detail.Members); });
        sw.CreateTable("Pools",
                       poolResults.SelectMany(r => r.Members.Select(member => new
                       {
                           Pool = r.pool.Id,
                           member.Type,
                           member.Node,
                           member.VmId,
                           member.Storage,
                           member.Status,
                           DescriptionWrap = member.Description,
                           CommentWrap = r.pool.Comment,
                       })).ToList(),
                       tbl =>
                       {
                           sw.ApplyNodeLinks(tbl);
                           sw.ApplyVmIdLinks(tbl);
                           sw.ApplyStorageLinks(tbl);
                       });

        ReportGlobal("Cluster: HA");
        sw.CreateTable("HA Resources",
                       haResourcesTask.Result.Select(a => new
                       {
                           a.Sid,
                           a.Type,
                           a.State,
                           a.Group,
                           a.Failback,
                           a.MaxRestart,
                           a.MaxRelocate,
                           a.Comment
                       }));

        if (haGroupsSupported && haGroupsTask != null)
        {
            sw.CreateTable("HA Groups",
                           haGroupsTask.Result.Select(a => new
                           {
                               a.Group,
                               a.Nodes,
                               a.Nofailback,
                               a.Restricted,
                               a.Comment
                           }));
        }

        sw.CreateTable("HA Status",
                       haStatusTask.Result.Select(a => new
                       {
                           a.Id,
                           a.Type,
                           a.Status,
                           a.Node,
                           a.Sid,
                           a.State,
                           a.CrmState,
                           a.RequestState,
                           QuorateFlag = ToX(a.Quorate),
                           FailbackFlag = ToX(a.Failback),
                           a.MaxRelocate,
                           a.MaxRestart,
                           Timestamp = FromUnixTime(a.Timestamp),
                       }));

        sw.WriteIndex();
        sw.AdjustColumns();

        return 1;
    }
}
