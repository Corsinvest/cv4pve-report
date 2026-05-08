/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Writers.Json;

/// <summary>
/// Section writer that captures the engine's calls (AddTable / AddKeyValue / AddKeyValueRow)
/// into structured data ready for serialisation. The parent <see cref="JsonReportWriter"/>
/// post-processes these blocks at SaveAsync time to produce the multi-file JSON layout.
/// </summary>
internal sealed class JsonSectionWriter(string name) : ISectionWriter
{
    public string Name { get; } = name;

    /// <summary>Ordered list of "blocks" captured for this section (KeyValue / KeyValueRow / Table).</summary>
    public List<JsonBlock> Blocks { get; } = [];

    /// <summary>HTML/XLSX have a Back link concept; ignored in JSON.</summary>
    public void AddBackLink(string label, string linkKey) { }

    public void AddKeyValue(string title, IDictionary<string, object?> items)
        => Blocks.Add(new JsonBlock.KeyValue(title, NormaliseKeyValue(items)));

    public void AddKeyValueRow(params (string Title, IDictionary<string, object?> Items)[] blocks)
    {
        foreach (var (title, items) in blocks)
        {
            Blocks.Add(new JsonBlock.KeyValue(title, NormaliseKeyValue(items)));
        }
    }

    public ITableHandle AddTable<T>(string? title, IEnumerable<T> data, TableOptions<T>? options = null)
    {
        var rows = data is IList<T> list ? list : [.. data];
        var serialisable = rows.Select(r => NormaliseRow(r)).Cast<object?>().ToList();
        Blocks.Add(new JsonBlock.Table(title, serialisable));
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
            var (suffix, _) = ColumnNameSuffix.Parse(p.Name);
            var raw = p.GetValue(row);
            var (key, value) = TransformByConvention(p.Name, raw, suffix);
            result[ToCamelCase(key)] = value;
        }

        return result;
    }

    private static IDictionary<string, object?> NormaliseKeyValue(IDictionary<string, object?> items)
    {
        var result = new Dictionary<string, object?>(items.Count);
        foreach (var (k, v) in items)
        {
            // Key-value blocks use display-style keys ("VM ID", "On Boot"). Keep as-is.
            result[k] = v;
        }
        return result;
    }

    private static (string Key, object? Value) TransformByConvention(string name, object? raw, ColumnSuffix suffix)
        => suffix switch
        {
            // Flag columns are pre-formatted by the engine as "X" / "" strings — turn them back into booleans.
            ColumnSuffix.Flag => (name[..^4], raw is string s ? s.Length > 0 : raw is bool b && b),
            // Wrap columns are just text with multi-line content — keep value, strip the suffix from the key.
            ColumnSuffix.Wrap => (name[..^4], raw),
            // GB / MB / Pct / Date / None: keep the raw value as the engine produced it.
            _ => (name, raw),
        };

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0])) { return name; }
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}

/// <summary>Discriminated record for the kinds of block a section can carry.</summary>
internal abstract record JsonBlock
{
    public sealed record KeyValue(string Title, IDictionary<string, object?> Items) : JsonBlock;
    public sealed record Table(string? Title, IList<object?> Rows) : JsonBlock;
}
