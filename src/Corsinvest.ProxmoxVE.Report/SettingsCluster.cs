/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Cluster settings
/// </summary>
public class SettingsCluster
{
    /// <summary>
    /// Include cluster overview sheet (users, roles, ACL, backup jobs)
    /// </summary>
    public bool IncludeSheet { get; set; } = true;

    /// <summary>
    /// Cluster log settings
    /// </summary>
    public SettingsClusterLog Log { get; set; } = new();

    /// <summary>
    /// Include cluster tasks sheet
    /// </summary>
    public bool IncludeTasksSheet { get; set; } = true;
}
