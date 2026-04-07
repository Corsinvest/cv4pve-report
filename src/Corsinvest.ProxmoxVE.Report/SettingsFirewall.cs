/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Firewall settings
/// </summary>
public class SettingsFirewall
{
    /// <summary>
    /// Include firewall rules, aliases, ipsets and log
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of firewall log lines to return (0 = unlimited)
    /// </summary>
    public int LogMaxCount { get; set; }

    internal int? Limit
        => LogMaxCount > 0
            ? LogMaxCount
            : null;

    /// <summary>
    /// Display firewall log since this date
    /// </summary>
    public DateOnly? LogSince { get; set; }

    /// <summary>
    /// Display firewall log until this date
    /// </summary>
    public DateOnly? LogUntil { get; set; }

    internal int? SinceUnix
        => LogSince.HasValue
            ? (int)new DateTimeOffset(LogSince.Value.ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds()
            : null;

    internal int? UntilUnix
        => LogUntil.HasValue
            ? (int)new DateTimeOffset(LogUntil.Value.ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds()
            : null;

    /// <summary>
    /// Max parallel requests when fetching Firewall data (1 = sequential)
    /// </summary>
    public int MaxParallelRequests { get; set; } = 5;
}
