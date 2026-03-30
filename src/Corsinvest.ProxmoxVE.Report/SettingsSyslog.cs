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
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Maximum number of lines to return (0 = unlimited)
    /// </summary>
    public int MaxCount { get; set; } = 500;

    /// <summary>
    /// Filter by service name (e.g. pvedaemon, pveproxy)
    /// </summary>
    public string Service { get; set; } = "";

    /// <summary>
    /// Display log since this date (e.g. 2024-01-01)
    /// </summary>
    public DateOnly? Since { get; set; }

    /// <summary>
    /// Display log until this date (e.g. 2024-01-01)
    /// </summary>
    public DateOnly? Until { get; set; }
}
