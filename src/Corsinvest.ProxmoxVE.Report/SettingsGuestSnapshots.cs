/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Snapshots settings
/// </summary>
public class SettingsGuestSnapshots
{
    /// <summary>
    /// Enable snapshots
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Max parallel requests when fetching snapshots (1 = sequential)
    /// </summary>
    public int MaxParallelRequests { get; set; } = 5;
}
