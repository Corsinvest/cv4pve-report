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
    /// Include storage content (ISO, templates, disk images)
    /// </summary>
    public bool IncludeContent { get; set; } = true;

    /// <summary>
    /// Include backup files
    /// </summary>
    public bool IncludeBackups { get; set; } = true;

    /// <summary>
    /// Include RRD metrics data
    /// </summary>
    public SettingsRrdData RrdData { get; set; } = new();
}
