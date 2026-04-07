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
    public SettingsRrdData RrdData { get; set; } = new()
    {
        Enabled = false,
        MaxParallelRequests = 5
    };

    /// <summary>
    /// Task history settings
    /// </summary>
    public SettingsTask Tasks { get; set; } = new();

    /// <summary>
    /// Include snapshots
    /// </summary>
    public SettingsGuestSnapshots Snapshots { get; set; } = new();

    /// <summary>
    /// Include QEMU agent info (network interfaces and filesystem info) — only for running VMs with agent enabled
    /// </summary>
    public bool IncludeQemuAgent { get; set; } = true;
}
