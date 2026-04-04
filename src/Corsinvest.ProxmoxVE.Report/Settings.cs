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

    /// <summary>
    /// Whether to skip empty collections (e.g. no snapshots) or include them as empty tables in the report.
    /// </summary>
    public bool SkipEmptyTables { get; set; } = true;

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
            Disk = new() { IncludeSmartData = false, IncludeZfs = false, IncludeDirectory = false },
            Firewall = new() { Enabled = false },
            IncludeSslCertificates = false,
            IncludeAptRepositories = false,
            IncludeAptUpdates = false,
            IncludeAptVersions = false,
            Tasks = new() { Enabled = false },
            RrdData = new() { Enabled = false },
            Syslog = new() { Enabled = false },
        },
        Guest = new()
        {
            Firewall = new() { Enabled = false },
            IncludeSnapshots = false,
            IncludeBackups = false,
            IncludeQemuAgent = false,
            Tasks = new() { Enabled = false },
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
    public static Settings Full()
    {
        var lastWeek = DateOnly.FromDateTime(DateTime.Now.AddDays(-3));
        return new()
        {
            Cluster = new()
            {
                AuditLog = new() { Enabled = true, MaxCount = 1000 },
            },
            Node = new()
            {
                Disk = new() { IncludeSmartData = true },
                IncludeAptVersions = true,
                RrdData = new() { TimeFrame = Api.Shared.Models.Common.RrdDataTimeFrame.Week },
                Firewall = new() { LogMaxCount = 1000, LogSince = lastWeek },
                Syslog = new()
                {
                    Enabled = true,
                    MaxEntries = 1000,
                    // Since = lastWeek
                },
            },
            Guest = new()
            {
                RrdData = new() { TimeFrame = Api.Shared.Models.Common.RrdDataTimeFrame.Week },
                Firewall = new() { LogMaxCount = 1000, LogSince = lastWeek },
            },
            Storage = new()
            {
                RrdData = new() { TimeFrame = Api.Shared.Models.Common.RrdDataTimeFrame.Week },
            },
        };
    }
}
