/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text;

namespace Corsinvest.ProxmoxVE.Report.Writers.Json;

/// <summary>
/// Helpers for turning human-readable display strings (as authored for Excel/HTML
/// rendering) into JSON-friendly identifier keys.
/// </summary>
internal static class JsonKey
{
    /// <summary>
    /// Translate a display string into a camelCase JSON key. Examples:
    ///   "Services"          → "services"
    ///   "SSL Certificates"  → "sslCertificates"
    ///   "Agent OS Info"     → "agentOSInfo"
    ///   "VM ID"             → "vmID"
    ///   "CPU Usage %"       → "cpuUsage"
    ///   "On Boot"           → "onBoot"
    ///   "/etc/hosts"        → "etcHosts"
    /// Symbols (% / parentheses / dashes / slashes) are dropped; the first word is
    /// lowercased; subsequent words have their first letter uppercased while the
    /// rest of each word is preserved (so common acronyms like ID/GB/MB/SSL/OS
    /// keep their casing).
    /// </summary>
    public static string FromDisplay(string display)
    {
        if (string.IsNullOrEmpty(display)) { return display; }

        var normalised = display.Replace("%", " ")
                                .Replace("(", " ")
                                .Replace(")", " ")
                                .Replace("/", " ")
                                .Replace("-", " ");

        var words = normalised.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) { return display; }

        var sb = new StringBuilder();
        sb.Append(words[0].ToLowerInvariant());
        for (var i = 1; i < words.Length; i++)
        {
            sb.Append(char.ToUpperInvariant(words[i][0]));
            if (words[i].Length > 1) { sb.Append(words[i][1..]); }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Property-name → JSON key. The engine already authored property names in
    /// PascalCase / camelCase (e.g. "VmId", "memoryUsageGB"), so the rules here
    /// are simpler than for display strings: only the first character is folded
    /// to lower-case.
    /// </summary>
    public static string FromPropertyName(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0])) { return name; }
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
