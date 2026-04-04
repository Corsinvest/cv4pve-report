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

    /// <summary>
    /// Display firewall log since this date
    /// </summary>
    public DateOnly? LogSince { get; set; }

    /// <summary>
    /// Display firewall log until this date
    /// </summary>
    public DateOnly? LogUntil { get; set; }

    internal int? LogSinceUnix
        => LogSince.HasValue
            ? (int)new DateTimeOffset(LogSince.Value.ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds()
            : null;

    internal int? LogUntilUnix
        => LogUntil.HasValue
            ? (int)new DateTimeOffset(LogUntil.Value.ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds()
            : null;
}
