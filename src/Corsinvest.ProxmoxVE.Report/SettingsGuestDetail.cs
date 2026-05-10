/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Per-VM/CT detail settings
/// </summary>
public class SettingsGuestDetail
{
    /// <summary>
    /// Enable per-VM/CT detail section
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Task history settings
    /// </summary>
    public SettingsTask Tasks { get; set; } = new();

    /// <summary>
    /// Include firewall log
    /// </summary>
    public bool IncludeFirewallLog { get; set; } = true;
}
