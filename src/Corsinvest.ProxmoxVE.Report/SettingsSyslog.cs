/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Syslog settings for node
/// </summary>
public class SettingsSyslog
{
    /// <summary>
    /// Include syslog
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Maximum number of entries to return (0 = 500). Ignored when Since is set.
    /// </summary>
    public int MaxCount { get; set; } = 500;

    // Without a limit, until alone makes the node read its whole journal and the call times out.
    internal int? Limit
        => SinceUnix.HasValue
            ? null
            : MaxCount > 0
                ? MaxCount
                : 500;

    /// <summary>
    /// Display Syslog since this date
    /// </summary>
    public DateOnly? Since { get; set; }

    /// <summary>
    /// Display Syslog until this date
    /// </summary>
    public DateOnly? Until { get; set; }

    internal int? SinceUnix
        => Since.HasValue
            ? (int)new DateTimeOffset(Since.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds()
            : null;

    internal int? UntilUnix
        => Until.HasValue
            ? (int)new DateTimeOffset(Until.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero).ToUnixTimeSeconds()
            : null;
}
