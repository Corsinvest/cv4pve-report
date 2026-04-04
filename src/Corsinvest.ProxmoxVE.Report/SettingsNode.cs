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
    /// Task history settings
    /// </summary>
    public SettingsTask Tasks { get; set; } = new();

    /// <summary>
    /// Disk settings
    /// </summary>
    public SettingsDisk Disk { get; set; } = new();

    /// <summary>
    /// Firewall settings
    /// </summary>
    public SettingsFirewall Firewall { get; set; } = new();

    /// <summary>
    /// Include SSL certificates
    /// </summary>
    public bool IncludeSslCertificates { get; set; } = true;

    /// <summary>
    /// Include APT available updates
    /// </summary>
    public bool IncludeAptUpdates { get; set; } = true;

    /// <summary>
    /// Include APT repositories
    /// </summary>
    public bool IncludeAptRepositories { get; set; } = true;

    /// <summary>
    /// Include APT installed package versions
    /// </summary>
    public bool IncludeAptVersions { get; set; }


    /// <summary>
    /// Include replication jobs
    /// </summary>
    public bool IncludeReplication { get; set; } = true;

    /// <summary>
    /// Syslog settings
    /// </summary>
    public SettingsSyslog Syslog { get; set; } = new();
}
