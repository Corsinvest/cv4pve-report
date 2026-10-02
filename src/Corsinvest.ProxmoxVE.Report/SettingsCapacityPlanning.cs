/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Capacity planning settings
/// </summary>
public class SettingsCapacityPlanning
{
    /// <summary>
    /// Enable the Capacity Planning section: average and peak usage per guest and node, storage growth.
    /// Built on the RRD data of each scope (guest, node, storage), which must be enabled, and reads
    /// the RRD data of guests and nodes a second time with the other consolidation function.
    /// </summary>
    public bool Enabled { get; set; }
}
