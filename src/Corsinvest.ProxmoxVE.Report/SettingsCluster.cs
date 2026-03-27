/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Cluster settings
/// </summary>
public class SettingsCluster
{
    /// <summary>
    /// Include cluster options
    /// </summary>
    public bool IncludeOptions { get; set; } = true;

    /// <summary>
    /// Include security: users, API tokens, TFA, groups, roles, ACL, permissions, domains
    /// </summary>
    public bool IncludeSecurity { get; set; } = true;

    /// <summary>
    /// Include firewall rules
    /// </summary>
    public bool IncludeFirewall { get; set; } = true;

    /// <summary>
    /// Include backup jobs
    /// </summary>
    public bool IncludeBackupJobs { get; set; } = true;

    /// <summary>
    /// Include replication jobs
    /// </summary>
    public bool IncludeReplication { get; set; } = true;

    /// <summary>
    /// Include cluster-level storage list
    /// </summary>
    public bool IncludeStorages { get; set; } = true;

    /// <summary>
    /// Include metric servers
    /// </summary>
    public bool IncludeMetricServers { get; set; } = true;

    /// <summary>
    /// Include SDN zones, vnets and controllers
    /// </summary>
    public bool IncludeSdn { get; set; } = true;

    /// <summary>
    /// Include hardware mappings (directory, PCI, USB)
    /// </summary>
    public bool IncludeMapping { get; set; } = true;

    /// <summary>
    /// Include resource pools
    /// </summary>
    public bool IncludePools { get; set; } = true;
}
