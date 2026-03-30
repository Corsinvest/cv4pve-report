/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Task history settings
/// </summary>
public class SettingsTask
{
    /// <summary>
    /// Include task history
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Show only failed tasks
    /// </summary>
    public bool OnlyErrors { get; set; } = false;

    /// <summary>
    /// Maximum number of tasks to return (0 = unlimited)
    /// </summary>
    public int MaxCount { get; set; } = 0;

    /// <summary>
    /// Task source filter: all, local, active
    /// </summary>
    public string Source { get; set; } = "all";
}
