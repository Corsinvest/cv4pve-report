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
    /// Include storage content (ISO, templates, disk images) sheet
    /// </summary>
    public bool IncludeContentSheet { get; set; } = true;

    /// <summary>
    /// Include backup files sheet
    /// </summary>
    public bool IncludeBackupsSheet { get; set; } = true;

    /// <summary>
    /// Include RRD metrics data
    /// </summary>
    public SettingsRrdData RrdData { get; set; } = new();
}
