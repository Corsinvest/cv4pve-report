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
    public int LogMaxCount { get; set; } = 0;

    /// <summary>
    /// Display firewall log since this date
    /// </summary>
    public DateOnly? LogSince { get; set; }

    /// <summary>
    /// Display firewall log until this date
    /// </summary>
    public DateOnly? LogUntil { get; set; }
}
