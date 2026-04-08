/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Disk-related settings for node detail sheet
/// </summary>
public class SettingsNodeDisk
{
    /// <summary>
    /// Include disk detail: physical disk list, ZFS pools and directory mount points
    /// </summary>
    public bool IncludeDiskDetail { get; set; } = true;

    /// <summary>
    /// Include SMART health data per disk (one API call per disk — can be slow)
    /// </summary>
    public bool IncludeSmartData { get; set; }
}
