/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance;

/// <summary>
/// Categories of data that compliance checks can declare as required.
/// Producer sections populate the <see cref="ComplianceContext"/> only for the
/// kinds requested by at least one enabled check.
/// </summary>
internal enum ComplianceDataKind
{
    Users,
    Tfa,
    Acl,
    Groups,
    Domains,
    BackupJobs,
    Vms,
    Certificates,
    HaResources,
    ReplicationJobs,
    Nodes,
    MetricServers,
    FirewallClusterOptions,
    FirewallNodeOptions,
    FirewallRules,
    Pools,
    ClusterTasks,
    Storages,
    Roles,
    NodeNetworks,
    ClusterLog,
}
