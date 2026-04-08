/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;

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

    /// <summary>
    /// Firewall settings (global rules, aliases, ipsets across cluster/nodes/VMs/CTs)
    /// </summary>
    public SettingsFirewall Firewall { get; set; } = new();

    /// <summary>
    /// Max parallel requests when fetching (1 = sequential)
    /// </summary>
    public int MaxParallelRequests { get; set; } = 5;

    /// <summary>Fast profile — structure only, no heavy data.</summary>
    public static Settings Fast() => new()
    {
        Node = new()
        {
            Detail = new() { Enabled = false },
            RrdData = new() { Enabled = false },
            Syslog = new() { Enabled = false },
        },
        Guest = new()
        {
            IncludeSnapshotsSheet = false,
            IncludeDisksSheet = false,
            IncludePartitionsSheet = false,
            IncludeQemuAgent = false,
            Detail = new() { Enabled = false },
            RrdData = new() { Enabled = false },
        },
        Storage = new()
        {
            IncludeContentSheet = false,
            IncludeBackupsSheet = false,
            RrdData = new() { Enabled = false },
        },
        Firewall = new() { Enabled = false },
    };

    /// <summary>Standard profile — all except SMART data. Default.</summary>
    public static Settings Standard() => new();

    /// <summary>Full profile — everything enabled, RRD on week timeframe.</summary>
    public static Settings Full()
    {
        var lastWeek = DateOnly.FromDateTime(DateTime.Now.AddDays(-3));
        return new()
        {
            Cluster = new()
            {
                Log = new()
                {
                    Enabled = true,
                    MaxCount = 1000
                },
            },
            Firewall = new()
            {
                MaxCount = 1000,
                Since = lastWeek
            },
            Node = new()
            {
                Detail = new()
                {
                    Disk = new() { IncludeSmartData = true },
                },
                RrdData = new() { TimeFrame = RrdDataTimeFrame.Week },
                Syslog = new()
                {
                    Enabled = true,
                    MaxCount = 1000
                },
            },
            Guest = new()
            {
                RrdData = new()
                {
                    Enabled = true,
                    TimeFrame = RrdDataTimeFrame.Week
                },
            },
            Storage = new()
            {
                RrdData = new() { TimeFrame = RrdDataTimeFrame.Week },
            },
        };
    }
}
