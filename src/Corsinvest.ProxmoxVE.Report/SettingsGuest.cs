/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Guest (VM/CT) settings
/// </summary>
public class SettingsGuest
{
    /// <summary>
    /// VM/CT IDs filter. Use @all or comma-separated IDs.
    /// </summary>
    public string Ids { get; set; } = "@all";

    /// <summary>
    /// Include RRD metrics data
    /// </summary>
    public SettingsRrdData RrdData { get; set; } = new();

    /// <summary>
    /// Include task history
    /// </summary>
    public bool IncludeTasks { get; set; } = true;

    /// <summary>
    /// Include backup files
    /// </summary>
    public bool IncludeBackups { get; set; } = true;

    /// <summary>
    /// Include snapshots
    /// </summary>
    public bool IncludeSnapshots { get; set; } = true;

    /// <summary>
    /// Include firewall rules
    /// </summary>
    public bool IncludeFirewall { get; set; } = true;

    /// <summary>
    /// Include QEMU agent info (network interfaces and filesystem info) — only for running VMs with agent enabled
    /// </summary>
    public bool IncludeQemuAgent { get; set; } = true;
}
