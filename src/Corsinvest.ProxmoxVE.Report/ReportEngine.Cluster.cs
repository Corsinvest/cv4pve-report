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
        var sw = new SheetWriter(workbook.Worksheets.Add("Cluster"), _sheetLinks);

        var tableCount = 1  // Status
                       + (settings.Cluster.IncludeOptions ? 1 : 0)
                       + (settings.Cluster.IncludeSecurity ? 7 : 0)
                       + (settings.Cluster.IncludeFirewall ? 4 : 0)
                       + (settings.Cluster.IncludeBackupJobs ? 1 : 0)
                       + (settings.Cluster.IncludeReplication ? 1 : 0)
                       + (settings.Cluster.IncludeStorages ? 1 : 0)
                       + (settings.Cluster.IncludeMetricServers ? 1 : 0)
                       + (settings.Cluster.IncludeSdn ? 5 : 0)
                       + (settings.Cluster.IncludeMapping ? 3 : 0)
                       + (settings.Cluster.IncludePools ? 1 : 0)
                       + (settings.Cluster.IncludeHa ? 3 : 0);

        var clusterStatus = (await client.Cluster.Status.GetAsync()).FirstOrDefault(a => a.Type == "cluster");

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
                       (await client.Cluster.Status.GetAsync())
                        .Select(a => new
                        {
                            a.Id,
                            a.Name,
                            a.Type,
                            a.Nodes,
                            a.Version,
                            a.Quorate,
                            Level = NodeHelper.DecodeLevelSupport(a.Level),
                            a.IpAddress,
                            a.NodeId,
                            a.IsOnline
                        }));

        if (settings.Cluster.IncludeOptions)
        {
            ReportGlobal("Cluster: Options");
            var options = await client.Cluster.Options.GetAsync();
            sw.CreateTable("Options",
                           [new
                           {
                               options.Console,
                               options.Keyboard,
                               options.MacPrefix,
                               options.Description,
                               AllowedTags = string.Join(", ", options.AllowedTags ?? []),
                               MigrationType = options.Migration?.Type,
                               MigrationNetwork = options.Migration?.Network,
                           }]);
        }

        if (settings.Cluster.IncludeSecurity)
        {
            ReportGlobal("Cluster: Security");
            var users = await client.Access.Users.GetAsync(full: true);

            sw.CreateTable("Users",
                           users.Select(a => new
                           {
                               a.Id,
                               a.Enable,
                               a.Email,
                               Expire = DateTimeOffset.FromUnixTimeSeconds(a.Expire).DateTime
                           }));

            sw.CreateTable("API Tokens",
                           users.SelectMany(a => a.Tokens)
                                .Select(a => new
                                {
                                    a.Id,
                                    Expire = DateTimeOffset.FromUnixTimeSeconds(a.Expire).DateTime,
                                    PrivSeparated = a.Privsep == 1,
                                    a.Comment
                                }));

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
                               Privileges = a.Privileges.Replace(",", Environment.NewLine),
                               Special = a.Special == 1
                           }));

            sw.CreateTable("ACL",
                           (await client.Access.Acl.GetAsync()).Select(a => new
                           {
                               Id = a.Roleid,
                               a.Path,
                               UsersOrGroup = a.UsersGroupid,
                               Propagate = a.Propagate == 1,
                               a.Type
                           }));

            sw.CreateTable("Domains",
                           (await client.Access.Domains.GetAsync()).Select(a => new
                           {
                               a.Type,
                               a.Realm,
                               a.Comment
                           }));
        }

        if (settings.Cluster.IncludeFirewall)
        {
            ReportGlobal("Cluster: Firewall");
            AddFirewallRules(sw, await client.Cluster.Firewall.Rules.GetAsync());

            var fwOptions = await client.Cluster.Firewall.Options.GetAsync();
            sw.CreateTable("Firewall Options",
                           [new { fwOptions.Enable, fwOptions.PolicyIn, fwOptions.PolicyOut, fwOptions.LogRatelimit }]);

            AddFirewallAlias(sw, await client.Cluster.Firewall.Aliases.GetAsync());

            AddFirewallIpSet(sw, await client.Cluster.Firewall.Ipset.GetAsync());
        }

        if (settings.Cluster.IncludeBackupJobs)
        {
            ReportGlobal("Cluster: Backup Jobs");
            sw.CreateTable("Backup Jobs",
                           (await client.Cluster.Backup.GetAsync()).Select(a => new
                           {
                               a.Id,
                               a.Enabled,
                               a.All,
                               VmId = a.VmId.Replace(",", Environment.NewLine),
                               a.Mode,
                               a.Storage,
                               a.StartTime,
                               a.Mailto,
                               a.Pool,
                               a.DayOfWeek,
                               a.Compress,
                               a.Type,
                               a.Schedule,
                               NextRun = DateTimeOffset.FromUnixTimeSeconds(a.NextRun).DateTime,
                               a.Node
                           }));
        }

        if (settings.Cluster.IncludeReplication)
        {
            ReportGlobal("Cluster: Replication");
            sw.CreateTable("Replication",
                           (await client.Cluster.Replication.GetAsync()).Select(a => new
                           {
                               a.Id,
                               a.Schedule,
                               a.Type,
                               a.Guest,
                               a.Source,
                               a.Target,
                               a.Disable,
                               a.Rate
                           }),
                           tbl => sw.ApplyReplicationLinks(tbl));
        }

        if (settings.Cluster.IncludeStorages)
        {
            ReportGlobal("Cluster: Storages");
            sw.CreateTable("Storages",
                           (await client.Storage.GetAsync()).Select(a => new
                           {
                               a.Storage,
                               a.Type,
                               a.Content,
                               a.Shared,
                               a.Disable,
                               a.Path,
                               a.Nodes
                           }));
        }

        if (settings.Cluster.IncludeMetricServers)
        {
            ReportGlobal("Cluster: Metric Servers");
            sw.CreateTable("Metric Servers",
                           (await client.Cluster.Metrics.Server.GetAsync()).Select(a => new
                           {
                               a.Id,
                               a.Server,
                               a.Port,
                               a.Type,
                               a.Disable
                           }));
        }

        if (settings.Cluster.IncludeSdn)
        {
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
                               a.State
                           }));

            sw.CreateTable("SDN Vnets",
                           (await client.Cluster.Sdn.Vnets.GetAsync()).Select(a => new
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
            foreach (var vnet in await client.Cluster.Sdn.Vnets.GetAsync())
            {
                foreach (var subnet in await client.Cluster.Sdn.Vnets[vnet.Vnet].Subnets.GetAsync())
                {
                    subnets.Add(new
                    {
                        Vnet = vnet.Vnet,
                        subnet.Subnet,
                        subnet.Type,
                        subnet.Gateway,
                        subnet.Snat,
                    });
                }
            }
            sw.CreateTable("SDN Subnets", subnets);
        }

        if (settings.Cluster.IncludeMapping)
        {
            ReportGlobal("Cluster: Mapping");
            sw.CreateTable("Mapping Dir",
                           (await client.Cluster.Mapping.Dir.GetAsync()).Select(a => new
                           {
                               a.Id,
                               a.Description,
                               Map = a.Map.JoinAsString(Environment.NewLine)
                           }));

            sw.CreateTable("Mapping PCI",
                           (await client.Cluster.Mapping.Pci.GetAsync()).Select(a => new
                           {
                               a.Id,
                               a.Description,
                               Map = a.Map.JoinAsString(Environment.NewLine)
                           }));

            sw.CreateTable("Mapping USB",
                           (await client.Cluster.Mapping.Usb.GetAsync()).Select(a => new
                           {
                               a.Id,
                               a.Description,
                               Map = a.Map.JoinAsString(Environment.NewLine)
                           }));
        }

        if (settings.Cluster.IncludePools)
        {
            ReportGlobal("Cluster: Pools");
            var poolItems = new List<dynamic>();
            foreach (var pool in await client.Pools.GetAsync())
            {
                foreach (var member in (await client.Pools[pool.Id].GetAsync()).Members)
                {
                    poolItems.Add(new
                    {
                        Pool = pool.Id,
                        pool.Comment,
                        member.Type,
                        member.Description,
                        member.VmId,
                        member.Storage,
                        member.Node,
                        member.Status,
                    });
                }
            }

            sw.CreateTable("Pools", poolItems, tbl =>
            {
                sw.ApplyNodeLinks(tbl);
                sw.ApplyVmIdLinks(tbl);
                sw.ApplyStorageLinks(tbl);
            });
        }

        if (settings.Cluster.IncludeHa)
        {
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

            sw.CreateTable("HA Groups",
                           (await client.Cluster.Ha.Groups.GetAsync()).Select(a => new
                           {
                               a.Group,
                               a.Nodes,
                               a.Nofailback,
                               a.Restricted,
                               a.Comment
                           }));

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
                               a.Quorate,
                           }));
        }

        sw.WriteIndex();
        sw.AdjustColumns();
    }
}
