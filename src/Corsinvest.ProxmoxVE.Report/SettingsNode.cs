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
    public SettingsRrdData RrdData { get; set; } = new() { MaxParallelRequests = 3 };

    /// <summary>
    /// Task history settings
    /// </summary>
    public SettingsTask Tasks { get; set; } = new();

    /// <summary>
    /// Disk settings
    /// </summary>
    public SettingsDisk Disk { get; set; } = new();

    /// <summary>
    /// Include APT repositories, available updates and installed package versions
    /// </summary>
    public bool IncludeApt { get; set; } = true;

    /// <summary>
    /// Include replication jobs global sheet
    /// </summary>
    public bool IncludeReplication { get; set; } = true;

    /// <summary>
    /// Syslog settings
    /// </summary>
    public SettingsSyslog Syslog { get; set; } = new();
}
