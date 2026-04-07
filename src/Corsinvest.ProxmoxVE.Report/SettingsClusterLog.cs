/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Cluster log settings
/// </summary>
public class SettingsClusterLog
{
    /// <summary>
    /// Enable cluster log sheet
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Maximum number of entries to return (0 = unlimited)
    /// </summary>
    public int MaxCount { get; set; }
}
