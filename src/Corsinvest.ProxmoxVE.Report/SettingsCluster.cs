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
    /// Include cluster overview (users, roles, ACL, backup jobs)
    /// </summary>
    public bool Include { get; set; } = true;

    /// <summary>
    /// Cluster log settings
    /// </summary>
    public SettingsClusterLog Log { get; set; } = new();

    /// <summary>
    /// Include cluster tasks
    /// </summary>
    public bool IncludeTasks { get; set; } = true;
}
