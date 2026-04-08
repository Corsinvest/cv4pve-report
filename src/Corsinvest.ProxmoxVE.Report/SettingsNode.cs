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
    /// Per-node detail sheet settings
    /// </summary>
    public SettingsNodeDetail Detail { get; set; } = new();

    /// <summary>
    /// Include RRD metrics data
    /// </summary>
    public SettingsRrdData RrdData { get; set; } = new();

    /// <summary>
    /// Include replication jobs global sheet
    /// </summary>
    public bool IncludeReplicationSheet { get; set; } = true;

    /// <summary>
    /// Syslog settings
    /// </summary>
    public SettingsSyslog Syslog { get; set; } = new();
}
