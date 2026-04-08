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
    public int MaxCount { get; set; }

    internal int? Limit
        => MaxCount > 0
            ? MaxCount
            : null;

    /// <summary>
    /// Display firewall log since this date
    /// </summary>
    public DateOnly? Since { get; set; }

    /// <summary>
    /// Display firewall log until this date
    /// </summary>
    public DateOnly? Until { get; set; }

    internal int? SinceUnix
        => Since.HasValue
            ? (int)new DateTimeOffset(Since.Value.ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds()
            : null;

    internal int? UntilUnix
        => Until.HasValue
            ? (int)new DateTimeOffset(Until.Value.ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds()
            : null;
}
