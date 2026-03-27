/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Node settings
/// </summary>
public class SettingsNode
{
    /// <summary>
    /// Node names filter. Use @all or comma-separated names (wildcards supported).
    /// </summary>
    public string Names { get; set; } = "@all";

    /// <summary>
    /// Include RRD metrics data
    /// </summary>
    public SettingsRrdData RrdData { get; set; } = new();

    /// <summary>
    /// Include task history
    /// </summary>
    public bool IncludeTasks { get; set; } = true;

    /// <summary>
    /// Include network interfaces
    /// </summary>
    public bool IncludeNetwork { get; set; } = true;

    /// <summary>
    /// Include disk list
    /// </summary>
    public bool IncludeDisks { get; set; } = true;

    /// <summary>
    /// Include SMART data for each disk (one API call per disk)
    /// </summary>
    public bool IncludeSmartData { get; set; } = true;

    /// <summary>
    /// Include system services
    /// </summary>
    public bool IncludeServices { get; set; } = true;

    /// <summary>
    /// Include firewall rules
    /// </summary>
    public bool IncludeFirewall { get; set; } = true;

    /// <summary>
    /// Include SSL certificates
    /// </summary>
    public bool IncludeSslCertificates { get; set; } = true;

    /// <summary>
    /// Include APT available updates
    /// </summary>
    public bool IncludeAptUpdates { get; set; } = true;

    /// <summary>
    /// Include APT installed package versions
    /// </summary>
    public bool IncludeAptVersions { get; set; } = true;

    /// <summary>
    /// Include replication jobs
    /// </summary>
    public bool IncludeReplication { get; set; } = true;
}
