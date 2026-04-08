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
    };

    /// <summary>
    /// Per-VM/CT detail sheet settings
    /// </summary>
    public SettingsGuestDetail Detail { get; set; } = new();

    /// <summary>
    /// Include snapshots global sheet
    /// </summary>
    public bool IncludeSnapshotsSheet { get; set; } = true;

    /// <summary>
    /// Include disks global sheet
    /// </summary>
    public bool IncludeDisksSheet { get; set; } = true;

    /// <summary>
    /// Include partitions global sheet (requires IncludeQemuAgent)
    /// </summary>
    public bool IncludePartitionsSheet { get; set; } = true;

    /// <summary>
    /// Include QEMU agent info (network interfaces and filesystem info) — only for running VMs with agent enabled
    /// </summary>
    public bool IncludeQemuAgent { get; set; } = true;
}
