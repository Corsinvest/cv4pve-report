/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.RegularExpressions;

namespace Corsinvest.ProxmoxVE.Report.Writers;

/// <summary>
/// Conventional column-name suffixes that drive shared formatting decisions
/// across writers (Excel cell format, HTML data-type/CSS class, display name).
/// </summary>
internal enum ColumnSuffix
{
    None,
    GB,
    MB,
    Pct,
    Wrap,
    DateOnly,
    Flag,
}

/// <summary>
/// Parses a property name written by report builders to detect a conventional
/// suffix (e.g. <c>MemoryUsageGB</c>, <c>CpuUsagePct</c>, <c>NextRunDate</c>) and
/// derive the human-readable display name. Single source of truth used by both
/// Excel and HTML writers; add a new suffix here and both formats pick it up.
/// </summary>
internal static partial class ColumnNameSuffix
{
    [GeneratedRegex("(?<=[a-z])([A-Z])|(?<=[A-Z])([A-Z][a-z])")]
    private static partial Regex PascalCaseSplitRegex();

    public static (ColumnSuffix Suffix, string DisplayName) Parse(string name)
    {
        if (EndsWith(name, "Pct")) { return (ColumnSuffix.Pct, PascalCaseToWords(name[..^3]) + " %"); }
        if (EndsWith(name, "GB")) { return (ColumnSuffix.GB, PascalCaseToWords(name)); }
        if (EndsWith(name, "MB")) { return (ColumnSuffix.MB, PascalCaseToWords(name)); }
        if (EndsWith(name, "Wrap")) { return (ColumnSuffix.Wrap, PascalCaseToWords(name[..^4])); }
        if (EndsWith(name, "Flag")) { return (ColumnSuffix.Flag, PascalCaseToWords(name[..^4])); }
        if (EndsWith(name, "Date")) { return (ColumnSuffix.DateOnly, PascalCaseToWords(name)); }
        return (ColumnSuffix.None, PascalCaseToWords(name));
    }

    public static string PascalCaseToWords(string name)
        => PascalCaseSplitRegex().Replace(name, " $1$2").Trim();

    private static bool EndsWith(string name, string suffix)
        => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
}
