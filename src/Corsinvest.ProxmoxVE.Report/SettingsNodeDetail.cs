/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Per-node detail sheet settings
/// </summary>
public class SettingsNodeDetail
{
    /// <summary>
    /// Enable detail sheets
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Disk settings
    /// </summary>
    public SettingsNodeDisk Disk { get; set; } = new();

    /// <summary>
    /// Task history settings
    /// </summary>
    public SettingsTask Tasks { get; set; } = new();

    /// <summary>
    /// Include APT repositories, available updates and installed package versions
    /// </summary>
    public bool IncludeApt { get; set; } = true;

    /// <summary>
    /// Include firewall log sheet
    /// </summary>
    public bool IncludeFirewallLog { get; set; } = true;

}
