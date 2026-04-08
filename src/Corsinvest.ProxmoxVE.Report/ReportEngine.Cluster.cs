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
    private async Task AddClusterDataAsync(XLWorkbook workbook)
    {
        if (!settings.Cluster.IncludeSheet) { return; }

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

        ReportGlobal("Cluster: Options");
        var options = await client.Cluster.Options.GetAsync();
        sw.CreateTable("Options",
                       [new
                           {
                               options.Console,
                               options.Keyboard,
                               options.MacPrefix,
                               DescriptionWrap = options.Description,
                               AllowedTags = ToNewLine(string.Join(",", options.AllowedTags ?? [])),
                               MigrationType = options.Migration?.Type,
                               MigrationNetwork = options.Migration?.Network,
                           }]);

        ReportGlobal("Cluster: Security");
        var users = await client.Access.Users.GetAsync(full: true);

        sw.CreateTable("Users",
                       users.Select(a => new
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
                       users.SelectMany(a => a.Tokens.Select(t => new
                       {
                           User = a.Id,
                           TokenId = t.Id,
                           Expire = FromUnixTime(t.Expire),
                           PrivSeparatedFlag = ToX(t.Privsep == 1),
                           CommentWrap = t.Comment
                       })));

        sw.CreateTable("Two-Factor Authentication",
                       (await client.Access.Tfa.GetAsync()).Select(t => new
                       {
                           User = t.UserId,
                           TfaTypes = string.Join(", ", t.Entries?.Select(e => e.Type).Distinct() ?? []),
                           TfaCount = t.Entries?.Count() ?? 0
                       }));

        sw.CreateTable("Groups",
                       (await client.Access.Groups.GetAsync()).Select(a => new
                       {
                           a.Id,
                           a.Users,
                           a.Comment
                       }));

        sw.CreateTable("Roles",
                       (await client.Access.Roles.GetAsync()).Select(a => new
                       {
                           a.Id,
                           Privileges = ToNewLine(a.Privileges),
                           SpecialFlag = ToX(a.Special == 1)
                       }));

        sw.CreateTable("ACL",
                       (await client.Access.Acl.GetAsync()).Select(a => new
                       {
                           a.Path,
                           UsersOrGroup = a.UsersGroupid,
                           a.Type,
                           Id = a.Roleid,
                           PropagateFlag = ToX(a.Propagate == 1),
                       }));

        sw.CreateTable("Domains",
                       (await client.Access.Domains.GetAsync()).Select(a => new
                       {
                           a.Realm,
                           a.Type,
                           a.Tfa,
                           a.Comment
                       }));

        if (settings.Firewall.Enabled)
        {
            ReportGlobal("Cluster: Firewall Options");
            var fwOptions = await client.Cluster.Firewall.Options.GetAsync();
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
                       (await client.Cluster.Backup.GetAsync()).Select(a => new
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
                       (await client.Cluster.Replication.GetAsync()).Select(a => new
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
                       (await client.Storage.GetAsync()).Select(a => new
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
                       (await client.Cluster.Metrics.Server.GetAsync()).Select(a => new
                       {
                           a.Id,
                           a.Server,
                           a.Port,
                           a.Type,
                           DisableFlag = ToX(a.Disable)
                       }));

        ReportGlobal("Cluster: SDN");
        sw.CreateTable("SDN Zones",
                       (await client.Cluster.Sdn.Zones.GetAsync()).Select(a => new
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

        var vnets = await client.Cluster.Sdn.Vnets.GetAsync();

        sw.CreateTable("SDN Vnets",
                       vnets.Select(a => new
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
                       (await client.Cluster.Sdn.Controllers.GetAsync()).Select(a => new
                       {
                           a.Controller,
                           a.Type,
                           a.Asn,
                           a.Peers,
                           a.Node,
                           a.State
                       }));

        sw.CreateTable("SDN Ipams",
                       (await client.Cluster.Sdn.Ipams.GetAsync()).Select(a => new
                       {
                           a.Ipam,
                           a.Type,
                       }));

        var subnets = new List<dynamic>();
        foreach (var vnet in vnets)
        {
            foreach (var subnet in await client.Cluster.Sdn.Vnets[vnet.Vnet].Subnets.GetAsync())
            {
                subnets.Add(new
                {
                    vnet.Vnet,
                    subnet.Subnet,
                    subnet.Type,
                    subnet.Gateway,
                    subnet.Snat,
                    subnet.DhcpDnsServer,
                    subnet.DnsZonePrefix,
                });
            }
        }
        sw.CreateTable("SDN Subnets", subnets);

        ReportGlobal("Cluster: Mapping");
        sw.CreateTable("Mapping Dir",
                       (await client.Cluster.Mapping.Dir.GetAsync()).Select(a => new
                       {
                           a.Id,
                           DescriptionWrap = a.Description,
                           MapWrap = a.Map.JoinAsString(Environment.NewLine)
                       }));

        sw.CreateTable("Mapping PCI",
                       (await client.Cluster.Mapping.Pci.GetAsync()).Select(a => new
                       {
                           a.Id,
                           DescriptionWrap = a.Description,
                           MapWrap = a.Map.JoinAsString(Environment.NewLine)
                       }));

        sw.CreateTable("Mapping USB",
                       (await client.Cluster.Mapping.Usb.GetAsync()).Select(a => new
                       {
                           a.Id,
                           DescriptionWrap = a.Description,
                           MapWrap = a.Map.JoinAsString(Environment.NewLine)
                       }));

        ReportGlobal("Cluster: Pools");
        var poolItems = new List<dynamic>();
        foreach (var pool in await client.Pools.GetAsync())
        {
            foreach (var member in (await client.Pools[pool.Id].GetAsync()).Members)
            {
                poolItems.Add(new
                {
                    Pool = pool.Id,
                    member.Type,
                    member.Node,
                    member.VmId,
                    member.Storage,
                    member.Status,
                    DescriptionWrap = member.Description,
                    CommentWrap = pool.Comment,
                });
            }
        }

        sw.CreateTable("Pools",
                       poolItems,
                       tbl =>
                       {
                           sw.ApplyNodeLinks(tbl);
                           sw.ApplyVmIdLinks(tbl);
                           sw.ApplyStorageLinks(tbl);
                       });

        ReportGlobal("Cluster: HA");
        sw.CreateTable("HA Resources",
                       (await client.Cluster.Ha.Resources.GetAsync()).Select(a => new
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

        if (haGroupsSupported)
        {
            sw.CreateTable("HA Groups",
                           (await client.Cluster.Ha.Groups.GetAsync()).Select(a => new
                           {
                               a.Group,
                               a.Nodes,
                               a.Nofailback,
                               a.Restricted,
                               a.Comment
                           }));
        }

        sw.CreateTable("HA Status",
                       (await client.Cluster.Ha.Status.Current.GetAsync()).Select(a => new
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
    }
}
