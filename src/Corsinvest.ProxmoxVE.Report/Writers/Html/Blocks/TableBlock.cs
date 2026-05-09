/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Globalization;
using System.Reflection;
using System.Text;

namespace Corsinvest.ProxmoxVE.Report.Writers.Html.Blocks;

/// <summary>
/// Titled table built from a list of POCO/anonymous-type rows.
/// Column metadata (numeric / percentage / date) is inferred from property name suffix
/// using the same conventions as the existing XLSX writer (Pct, GB, MB, Wrap, Flag).
/// </summary>
internal sealed class TableBlock<T> : IBlock
{
    private readonly IList<T> _rows;
    private readonly ColumnInfo[] _columns;

    /// <summary>Outgoing links: column name → mapper from row to link key.</summary>
    public IDictionary<string, Func<T, string?>>? ColumnLinks { get; init; }

    public TableBlock(string? title, IList<T> rows)
    {
        Title = title;
        _rows = rows;
        // For dynamic / object T (anonymous types passed as List<dynamic>) we must
        // inspect the runtime type of the first row, since typeof(T) returns no properties.
        var runtimeType = typeof(T) == typeof(object) && rows.Count > 0 && rows[0] != null
                            ? rows[0]!.GetType()
                            : typeof(T);
        _columns = BuildColumns(runtimeType);
    }

    public string? Title { get; }
    public string AnchorId { get; } = $"table-{Guid.NewGuid():N}";
    string? IBlock.AnchorId => Title == null ? null : AnchorId;

    public void Render(StringBuilder sb, Dictionary<string, string> links)
    {
        var headers = string.Concat(_columns.Select(RenderHeader));
        var body = string.Concat(_rows.Select(r => RenderRow(r, links)));
        var titleHtml = Title != null ? $"<h2>{HtmlEncoder.Text(Title)}</h2>\n  " : "";

        sb.Append($"""
            <section class="table-section" id="{AnchorId}">
              {titleHtml}<div class="table-scroll">
                <table class="data sortable">
                  <thead><tr>{headers}</tr></thead>
                  <tbody>
            {body}      </tbody>
                </table>
              </div>
            </section>

            """);
    }

    private static string RenderHeader(ColumnInfo col)
    {
        var dataType = col.Kind switch
        {
            ColumnKind.Number or ColumnKind.Percentage => " data-type=\"number\"",
            ColumnKind.DateTime or ColumnKind.DateOnly => " data-type=\"date\"",
            _ => "",
        };
        return $"<th{dataType}>{HtmlEncoder.Text(col.DisplayName)}</th>";
    }

    private string RenderRow(T row, Dictionary<string, string> links)
    {
        var cells = string.Concat(_columns.Select(col => RenderCell(row, col, links)));
        return $"      <tr>{cells}</tr>{Environment.NewLine}";
    }

    private string RenderCell(T row, ColumnInfo col, Dictionary<string, string> links)
    {
        var value = col.Property.GetValue(row);
        var classAttr = ClassFor(col.Kind);

        // Flag columns: ReportEngine pre-formats values as "X" (true) or "" (false).
        // Render a green check / dash instead of a literal "X" — easier to scan visually.
        if (col.Kind == ColumnKind.Flag)
        {
            var truthy = value is string s && s.Length > 0;
            var glyph = truthy ? "<span class=\"flag-yes\">✓</span>" : "<span class=\"flag-no\">·</span>";
            return $"<td{classAttr}>{glyph}</td>";
        }

        var text = HtmlEncoder.Text(FormatCell(value, col.Kind));

        if (ColumnLinks != null
            && ColumnLinks.TryGetValue(col.Name, out var mapper)
            && mapper(row) is { } linkKey
            && links.TryGetValue(linkKey, out var target))
        {
            return $"""<td{classAttr}><a href="{HtmlEncoder.PageHref(target)}">{text}</a></td>""";
        }

        return $"<td{classAttr}>{text}</td>";
    }

    private static string ClassFor(ColumnKind kind)
        => kind switch
        {
            ColumnKind.Number or ColumnKind.Percentage => " class=\"num\"",
            ColumnKind.Flag => " class=\"flag\"",
            ColumnKind.Wrap => " class=\"wrap\"",
            ColumnKind.DateTime or ColumnKind.DateOnly => " class=\"date\"",
            _ => "",
        };

    private static string FormatCell(object? value, ColumnKind kind) => value switch
    {
        null => "",
        bool b => b ? "Yes" : "No",
        DateTime d when kind == ColumnKind.DateOnly => d.ToString("yyyy-MM-dd"),
        DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss"),
        DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss"),
        double dbl when kind == ColumnKind.Percentage => (dbl * 100).ToString("0.00", CultureInfo.InvariantCulture) + "%",
        float fl when kind == ColumnKind.Percentage => (fl * 100).ToString("0.00", CultureInfo.InvariantCulture) + "%",
        double dbl => dbl.ToString("0.##", CultureInfo.InvariantCulture),
        float fl => fl.ToString("0.##", CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    private static ColumnInfo[] BuildColumns(Type rowType)
        => [.. rowType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(BuildColumn)];

    private static ColumnInfo BuildColumn(PropertyInfo p)
    {
        var (suffix, displayName) = ColumnNameSuffix.Parse(p.Name);

        var kind = suffix switch
        {
            ColumnSuffix.Pct => ColumnKind.Percentage,
            ColumnSuffix.GB or ColumnSuffix.MB => ColumnKind.Number,
            ColumnSuffix.Wrap => ColumnKind.Wrap,
            ColumnSuffix.Flag => ColumnKind.Flag,
            ColumnSuffix.DateOnly => ColumnKind.DateOnly,
            _ => IsNumeric(p.PropertyType) ? ColumnKind.Number
               : IsDate(p.PropertyType) ? ColumnKind.DateTime
               : ColumnKind.Text,
        };

        return new ColumnInfo(p, p.Name, displayName, kind);
    }

    private static bool IsNumeric(Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;
        return t == typeof(int) || t == typeof(long) || t == typeof(double)
            || t == typeof(float) || t == typeof(decimal) || t == typeof(short)
            || t == typeof(uint) || t == typeof(ulong) || t == typeof(ushort);
    }

    private static bool IsDate(Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;
        return t == typeof(DateTime) || t == typeof(DateTimeOffset);
    }

    private enum ColumnKind { Text, Number, Percentage, DateTime, DateOnly, Wrap, Flag }
    private sealed record ColumnInfo(PropertyInfo Property, string Name, string DisplayName, ColumnKind Kind);
}
