/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Storage content settings
/// </summary>
public class SettingsStorageContent
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
    /// Max parallel requests when fetching storage content (1 = sequential)
    /// </summary>
    public int MaxParallelRequests { get; set; } = 5;
}
