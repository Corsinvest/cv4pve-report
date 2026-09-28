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
    ///   "Memory GB"         → "memory"
    ///   "Root FS GB"        → "rootFS"
    ///   "On Boot"           → "onBoot"
    ///   "/etc/hosts"        → "etcHosts"
    ///   "S.M.A.R.T. Data"   → "smartData"
    /// Convention suffixes (<c>GB</c>, <c>MB</c>, <c>%</c>) and symbols
    /// (parentheses / dashes / slashes) are dropped; the first word is lower-cased;
    /// subsequent words have their first letter upper-cased while the rest is
    /// preserved (so common acronyms like ID/SSL/OS/FS keep their casing).
    /// </summary>
    public static string FromDisplay(string display)
    {
        if (string.IsNullOrEmpty(display)) { return display; }

        // Strip trailing unit / percentage suffix carried by KeyValue display labels.
        var trimmed = display;
        if (trimmed.EndsWith(" GB", StringComparison.Ordinal)) { trimmed = trimmed[..^3]; }
        else if (trimmed.EndsWith(" MB", StringComparison.Ordinal)) { trimmed = trimmed[..^3]; }

        // Dotted acronyms ("S.M.A.R.T.") collapse into one word.
        var normalised = trimmed.Replace(".", "")
                                .Replace("%", " ")
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
    /// PascalCase / camelCase (e.g. "VmId", "memoryUsageGB"), so only the leading
    /// run of capitals is folded, like camelCase does: "IP" → "ip",
    /// "IPAddress" → "ipAddress", "VmId" → "vmId".
    /// </summary>
    public static string FromPropertyName(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0])) { return name; }

        var upperRun = 0;
        while (upperRun < name.Length && char.IsUpper(name[upperRun])) { upperRun++; }

        // The last capital of an acronym run starts the next word ("IPAddress": the "A").
        var lowerCount = upperRun > 1 && upperRun < name.Length && char.IsLower(name[upperRun])
                            ? upperRun - 1
                            : upperRun;
        return name[..lowerCount].ToLowerInvariant() + name[lowerCount..];
    }
}
