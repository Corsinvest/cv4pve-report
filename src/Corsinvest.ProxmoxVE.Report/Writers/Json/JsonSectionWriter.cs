/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using static Corsinvest.ProxmoxVE.Report.Writers.Json.JsonBlock;

namespace Corsinvest.ProxmoxVE.Report.Writers.Json;

/// <summary>
/// Section writer that captures the engine's calls (AddTable / AddKeyValue / AddKeyValueRow)
/// into structured data ready for serialisation. The parent <see cref="JsonReportWriter"/>
/// post-processes these blocks at SaveAsync time to produce the multi-file JSON layout.
/// </summary>
internal sealed class JsonSectionWriter(string name) : ISectionWriter
{
    public string Name { get; } = name;
    public List<JsonBlock> Blocks { get; } = [];

    public void AddBackLink(string label, string linkKey) { }
    public void AddKeyValue(string title, IDictionary<string, object?> items) => Blocks.Add(new KeyValue(title, NormaliseKeyValue(items)));

    public void AddKeyValueRow(params (string Title, IDictionary<string, object?> Items)[] blocks)
    {
        foreach (var (title, items) in blocks)
        {
            Blocks.Add(new KeyValue(title, NormaliseKeyValue(items)));
        }
    }

    public ITableHandle AddTable<T>(string? title, IEnumerable<T> data, TableOptions<T>? options = null)
    {
        var rows = data is IList<T> list ? list : [.. data];
        var serialisable = rows.Select(r => NormaliseRow(r)).Cast<object?>().ToList();
        Blocks.Add(new Table(title, serialisable));
        return new JsonTableHandle(title ?? "", serialisable);
    }

    public void AppendData<T>(ITableHandle table, IEnumerable<T> data)
    {
        var handle = (JsonTableHandle)table;
        foreach (var row in data) { handle.Rows.Add(NormaliseRow(row)); }
    }

    public void Dispose() { }

    /// <summary>
    /// Decode the conventional column-name suffixes (Flag/Wrap/Pct/GB/MB/Date) into JSON-friendly
    /// values: flag-strings become booleans, "Wrap" suffix is stripped, etc. Mirrors what the
    /// XLSX/HTML writers do for header rendering, but applied to property names of the row.
    /// </summary>
    private static IDictionary<string, object?> NormaliseRow<T>(T row)
    {
        if (row is null) { return new Dictionary<string, object?>(); }

        var props = row.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var result = new Dictionary<string, object?>(props.Length);

        foreach (var p in props)
        {
            var (kind, _) = ColumnConvention.Parse(p);
            var raw = p.GetValue(row);
            var (key, value) = TransformByConvention(p.Name, raw, kind);
            result[JsonKey.FromPropertyName(key)] = value;
        }

        return result;
    }

    /// <summary>
    /// Convert key-value blocks into JSON-friendly keys. Display labels such as
    /// <c>"Memory GB"</c>, <c>"CPU Usage %"</c>, <c>"Root FS GB"</c>, <c>"VM ID"</c>
    /// are all slugified via <see cref="JsonKey.FromDisplay"/>, which strips the
    /// convention suffix (<c>" GB"</c> / <c>" MB"</c> / <c>" %"</c>) before camelCase.
    /// Boolean flag values (string <c>"X"</c> / <c>""</c>) are coerced to <c>bool</c>.
    /// </summary>
    private static IDictionary<string, object?> NormaliseKeyValue(IDictionary<string, object?> items)
    {
        var result = new Dictionary<string, object?>(items.Count);
        foreach (var (rawKey, value) in items)
        {
            var (kind, _) = ColumnConvention.Parse(rawKey);
            var transformedValue = kind == ColumnKind.Flag
                                    ? (value is string s ? s.Length > 0 : value is bool b && b)
                                    : value;
            result[JsonKey.FromDisplay(rawKey)] = transformedValue;
        }
        return result;
    }

    // Strips convention suffixes from JSON keys. Flag values ("X" / "") are also
    // turned back into real booleans. Other kinds keep the raw value the engine produced.
    private static (string Key, object? Value) TransformByConvention(string name, object? raw, ColumnKind kind)
        => kind switch
        {
            ColumnKind.Flag => (name[..^4], raw is string s ? s.Length > 0 : raw is bool b && b),
            ColumnKind.Wrap => (name[..^4], raw),
            ColumnKind.GB => (name[..^2], raw),
            ColumnKind.MB => (name[..^2], raw),
            ColumnKind.Percentage => (name[..^3], raw),
            _ => (name, raw),
        };
}

/// <summary>Discriminated record for the kinds of block a section can carry.</summary>
internal abstract record JsonBlock
{
    public sealed record KeyValue(string Title, IDictionary<string, object?> Items) : JsonBlock;
    public sealed record Table(string? Title, IList<object?> Rows) : JsonBlock;
}
