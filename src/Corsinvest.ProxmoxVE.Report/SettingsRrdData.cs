/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// RRD data settings
/// </summary>
public class SettingsRrdData
{
    /// <summary>
    /// Enable RRD data sheets
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// RRD time frame
    /// </summary>
    public RrdDataTimeFrame TimeFrame { get; set; } = RrdDataTimeFrame.Day;

    /// <summary>
    /// RRD consolidation function
    /// </summary>
    public RrdDataConsolidation Consolidation { get; set; } = RrdDataConsolidation.Average;

    /// <summary>
    /// Max parallel requests when fetching RRD data (1 = sequential)
    /// </summary>
    public int MaxParallelRequests { get; set; } = 5;
}
