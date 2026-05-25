/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Report.Compliance;
using Corsinvest.ProxmoxVE.Report.Helpers;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private async Task<int> AddClusterDataAsync()
    {
        if (!settings.Cluster.Include) { return 0; }

        using var sw = _writer.AddSection("Cluster");

        var allClusterStatus = await client.Cluster.Status.GetAsync();
        var clusterStatus = allClusterStatus.FirstOrDefault(a => a.Type == "cluster");

        sw.AddKeyValue("Cluster", new Dictionary<string, object?>
        {
            ["Name"] = clusterStatus?.Name ?? "-",
            ["Nodes"] = clusterStatus?.Nodes ?? 0,
            ["Version"] = clusterStatus?.Version ?? 0,
            ["Quorate"] = clusterStatus?.Quorate == 1 ? "Yes" : "No",
        });

        ReportGlobal("Cluster: Status");
        sw.AddTable("Status",
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

        ReportGlobal("Cluster: Fetching data");

        var optionsTask = client.Cluster.Options.GetAsync().ToSafeSingle(_issues, "Cluster", LinkKey.Cluster);
        var backupJobsTask = client.Cluster.Backup.GetAsync().ToSafeEnum(_issues, "Cluster", LinkKey.Cluster);
        var replicationTask = client.Cluster.Replication.GetAsync().ToSafeEnum(_issues, "Cluster", LinkKey.Cluster);
        var metricServersTask = client.Cluster.Metrics.Server.GetAsync().ToSafeEnum(_issues, "Cluster", LinkKey.Cluster);
        var mappingDirTask = client.Cluster.Mapping.Dir.GetAsync().ToSafeEnum(_issues, "Cluster", LinkKey.Cluster);
        var mappingPciTask = client.Cluster.Mapping.Pci.GetAsync().ToSafeEnum(_issues, "Cluster", LinkKey.Cluster);
        var mappingUsbTask = client.Cluster.Mapping.Usb.GetAsync().ToSafeEnum(_issues, "Cluster", LinkKey.Cluster);

        var fwOptionsTask = settings.Firewall.Enabled
                                ? client.Cluster.Firewall.Options.GetAsync().ToSafeSingle(_issues, "Cluster", LinkKey.Cluster)
                                : Task.FromResult<ClusterFirewallOptions?>(null);

        await Task.WhenAll(optionsTask, backupJobsTask, replicationTask, metricServersTask,
                           mappingDirTask, mappingPciTask, mappingUsbTask, fwOptionsTask);

        ReportGlobal("Cluster: Options");
        var options = optionsTask.Result ?? new ClusterOptions();
        sw.AddTable("Options",
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

        if (settings.Firewall.Enabled)
        {
            ReportGlobal("Cluster: Firewall Options");
            var fwOptions = fwOptionsTask.Result ?? new ClusterFirewallOptions();
            sw.AddTable("Firewall Options",
                        [new
                        {
                            fwOptions.Enable,
                            fwOptions.PolicyIn,
                            fwOptions.PolicyOut,
                            fwOptions.LogRatelimit
                        }]);

            if (_compliance.IsRequired(ComplianceDataKind.FirewallClusterOptions))
            {
                _compliance.Provide(ComplianceDataKind.FirewallClusterOptions,
                                    new[] { new Compliance.Models.FirewallClusterOptionsInfo(
                                        Enabled: fwOptions.Enable,
                                        PolicyIn: fwOptions.PolicyIn,
                                        PolicyOut: fwOptions.PolicyOut) });
            }
        }

        ReportGlobal("Cluster: Backup Jobs");
        if (_compliance.IsRequired(ComplianceDataKind.BackupJobs))
        {
            _compliance.Provide(ComplianceDataKind.BackupJobs,
                                backupJobsTask.Result.Select(a => new Compliance.Models.BackupJobInfo(
                                    Id: a.Id,
                                    Enabled: a.Enabled,
                                    All: a.All,
                                    VmIds: ParseVmIds(a.VmId),
                                    Pool: a.Pool,
                                    Node: a.Node,
                                    Storage: a.Storage,
                                    Schedule: a.Schedule)).ToList());
        }
        sw.AddTable("Backup Jobs",
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
        if (_compliance.IsRequired(ComplianceDataKind.ReplicationJobs))
        {
            _compliance.Provide(ComplianceDataKind.ReplicationJobs,
                                replicationTask.Result.Select(a => new Compliance.Models.ReplicationJobInfo(
                                    Id: a.Id,
                                    GuestVmId: long.TryParse(a.Guest, out var gid) ? gid : null,
                                    Target: a.Target,
                                    Schedule: a.Schedule,
                                    Disabled: a.Disable)).ToList());
        }
        sw.AddTable("Replication",
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
                    }).ToList(),
                    new TableOptions<dynamic>().WithReplicationLinks(nodeSelector: _ => null,
                                                                     vmIdSelector: r => r.Guest is long g ? g : null,
                                                                     sourceSelector: r => (string?)r.Source,
                                                                     targetSelector: r => (string?)r.Target));

        ReportGlobal("Cluster: Storages");
        sw.AddTable("Storages",
                    _storageConfigs.Select(a => new
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
        if (_compliance.IsRequired(ComplianceDataKind.MetricServers))
        {
            _compliance.Provide(ComplianceDataKind.MetricServers,
                                metricServersTask.Result.Select(a => new Compliance.Models.MetricServerInfo(
                                    Id: a.Id,
                                    Type: a.Type,
                                    Disabled: a.Disable)).ToList());
        }
        sw.AddTable("Metric Servers",
                    metricServersTask.Result.Select(a => new
                    {
                        a.Id,
                        a.Server,
                        a.Port,
                        a.Type,
                        DisableFlag = ToX(a.Disable)
                    }));

        ReportGlobal("Cluster: Mapping");
        sw.AddTable("Mapping Dir",
                    mappingDirTask.Result.Select(a => new
                    {
                        a.Id,
                        DescriptionWrap = a.Description,
                        MapWrap = a.Map.JoinAsString(Environment.NewLine)
                    }));

        sw.AddTable("Mapping PCI",
                    mappingPciTask.Result.Select(a => new
                    {
                        a.Id,
                        DescriptionWrap = a.Description,
                        MapWrap = a.Map.JoinAsString(Environment.NewLine)
                    }));

        sw.AddTable("Mapping USB",
                    mappingUsbTask.Result.Select(a => new
                    {
                        a.Id,
                        DescriptionWrap = a.Description,
                        MapWrap = a.Map.JoinAsString(Environment.NewLine)
                    }));

        return 1;
    }

    private static IReadOnlyList<long> ParseVmIds(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) { return []; }
        var ids = new List<long>();
        foreach (var token in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (long.TryParse(token, out var id)) { ids.Add(id); }
        }
        return ids;
    }
}
