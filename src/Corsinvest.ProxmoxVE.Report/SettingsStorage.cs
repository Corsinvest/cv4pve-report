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
    /// Storage content and backup settings
    /// </summary>
    public SettingsStorageContent Content { get; set; } = new();

    /// <summary>
    /// Include RRD metrics data
    /// </summary>
    public SettingsRrdData RrdData { get; set; } = new() { MaxParallelRequests = 5 };
}
