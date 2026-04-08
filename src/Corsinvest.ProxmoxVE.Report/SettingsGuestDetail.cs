/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Per-VM/CT detail sheet settings
/// </summary>
public class SettingsGuestDetail
{
    /// <summary>
    /// Enable detail sheets
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Task history settings
    /// </summary>
    public SettingsTask Tasks { get; set; } = new();
}
