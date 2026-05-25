/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Reflection;
using System.Text.RegularExpressions;

namespace Corsinvest.ProxmoxVE.Report.Writers;

/// <summary>
/// Classifies a property by its name suffix (<c>MemoryUsageGB</c>, <c>CpuUsagePct</c>,
/// <c>IsTemplateFlag</c>, <c>NextRunDate</c>, <c>DescriptionWrap</c>) and, when no
/// suffix matches, by its CLR type. Single source of truth used by every writer —
/// add a new <see cref="ColumnKind"/> here and the formats pick it up.
/// </summary>
internal static partial class ColumnConvention
{
    [GeneratedRegex("(?<=[a-z])([A-Z])|(?<=[A-Z])([A-Z][a-z])")]
    private static partial Regex PascalCaseSplitRegex();

    /// <summary>Name-only parsing — no CLR type, plain-text values fall through to <see cref="ColumnKind.Text"/>.</summary>
    public static (ColumnKind Kind, string DisplayName) Parse(string name)
    {
        // Human-authored labels ("Memory GB", "CPU Usage %") keep their casing verbatim;
        // PascalCase identifiers get the regex word-split.
        if (name.EndsWith(" %", StringComparison.Ordinal)) { return (ColumnKind.Percentage, name); }
        if (name.EndsWith(" GB", StringComparison.Ordinal)) { return (ColumnKind.GB, name); }
        if (name.EndsWith(" MB", StringComparison.Ordinal)) { return (ColumnKind.MB, name); }

        if (EndsWith(name, "Pct")) { return (ColumnKind.Percentage, PascalCaseToWords(name[..^3]) + " %"); }
        if (EndsWith(name, "GB")) { return (ColumnKind.GB, PascalCaseToWords(name)); }
        if (EndsWith(name, "MB")) { return (ColumnKind.MB, PascalCaseToWords(name)); }
        if (EndsWith(name, "Wrap")) { return (ColumnKind.Wrap, PascalCaseToWords(name[..^4])); }
        if (EndsWith(name, "Flag")) { return (ColumnKind.Flag, PascalCaseToWords(name[..^4])); }
        if (EndsWith(name, "Date")) { return (ColumnKind.DateOnly, PascalCaseToWords(name)); }
        if (EndsWith(name, "HealthScore")) { return (ColumnKind.HealthScore, PascalCaseToWords(name[..^"Score".Length])); }
        if (EndsWith(name, "ScoreBadge")) { return (ColumnKind.ScoreBadge, PascalCaseToWords(name[..^"Badge".Length]) + " %"); }
        if (name.Equals("Status", StringComparison.Ordinal)) { return (ColumnKind.StatusBadge, "Status"); }

        // camelCase ("vCPUs") and labels with spaces ("VM ID") are display labels too.
        var isDisplayLabel = name.Contains(' ') || (name.Length > 0 && char.IsLower(name[0]));
        return (ColumnKind.Text, isDisplayLabel ? name : PascalCaseToWords(name));
    }

    /// <summary>Suffix first; if the name doesn't carry one, classify by the CLR type.</summary>
    public static (ColumnKind Kind, string DisplayName) Parse(PropertyInfo p)
    {
        var result = Parse(p.Name);
        if (result.Kind != ColumnKind.Text) { return result; }
        if (IsNumeric(p.PropertyType)) { return (ColumnKind.Number, result.DisplayName); }
        if (IsDate(p.PropertyType)) { return (ColumnKind.DateTime, result.DisplayName); }
        return result;
    }

    public static string PascalCaseToWords(string name)
        => PascalCaseSplitRegex().Replace(name, " $1$2").Trim();

    private static bool EndsWith(string name, string suffix)
        => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);

    private static readonly HashSet<Type> NumericTypes =
    [
        typeof(int), typeof(long), typeof(double), typeof(float), typeof(decimal),
        typeof(short), typeof(uint), typeof(ulong), typeof(ushort),
    ];

    private static readonly HashSet<Type> DateTypes = [typeof(DateTime), typeof(DateTimeOffset),];
    private static bool IsNumeric(Type t) => NumericTypes.Contains(Nullable.GetUnderlyingType(t) ?? t);
    private static bool IsDate(Type t) => DateTypes.Contains(Nullable.GetUnderlyingType(t) ?? t);
}
