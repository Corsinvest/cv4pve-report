/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Settings for report generation
/// </summary>
public class Settings
{
    /// <summary>
    /// Cluster settings
    /// </summary>
    public SettingsCluster Cluster { get; set; } = new();

    /// <summary>
    /// Node settings
    /// </summary>
    public SettingsNode Node { get; set; } = new();

    /// <summary>
    /// Guest (VM/CT) settings
    /// </summary>
    public SettingsGuest Guest { get; set; } = new();

    /// <summary>
    /// Storage settings
    /// </summary>
    public SettingsStorage Storage { get; set; } = new();

    /// <summary>Fast profile — structure only, no heavy data.</summary>
    public static Settings Fast() => new()
    {
        Cluster = new()
        {
            IncludeFirewall = false,
            IncludeMetricServers = false,
            IncludeSdn = false,
            IncludeMapping = false,
        },
        Node = new()
        {
            IncludeSmartData = false,
            IncludeServices = false,
            IncludeFirewall = false,
            IncludeSslCertificates = false,
            IncludeAptUpdates = false,
            IncludeAptVersions = false,
            IncludeTasks = false,
            RrdData = new() { Enabled = false },
        },
        Guest = new()
        {
            IncludeFirewall = false,
            IncludeSnapshots = false,
            IncludeBackups = false,
            IncludeQemuAgent = false,
            IncludeTasks = false,
            RrdData = new() { Enabled = false },
        },
        Storage = new()
        {
            RrdData = new() { Enabled = false },
        },
    };

    /// <summary>Standard profile — all except SMART, APT versions and tasks. Default.</summary>
    public static Settings Standard() => new();

    /// <summary>Full profile — everything enabled, RRD on week timeframe.</summary>
    public static Settings Full() => new()
    {
        Node = new()
        {
            RrdData = new() { TimeFrame = Api.Shared.Models.Common.RrdDataTimeFrame.Week },
        },
        Guest = new()
        {
            RrdData = new() { TimeFrame = Api.Shared.Models.Common.RrdDataTimeFrame.Week },
        },
        Storage = new()
        {
            RrdData = new() { TimeFrame = Api.Shared.Models.Common.RrdDataTimeFrame.Week },
        },
    };
}
