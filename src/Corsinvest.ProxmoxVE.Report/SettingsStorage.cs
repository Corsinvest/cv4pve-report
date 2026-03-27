/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Storage settings
/// </summary>
public class SettingsStorage
{
    /// <summary>
    /// Storage names filter. Use @all or comma-separated names (wildcards supported).
    /// </summary>
    public string Names { get; set; } = "@all";

    /// <summary>
    /// Include RRD metrics data
    /// </summary>
    public SettingsRrdData RrdData { get; set; } = new();
}
