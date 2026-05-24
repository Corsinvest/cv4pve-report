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
    /// Convert key-value blocks (as authored for Excel/HTML rendering, with
    /// human-readable keys like "VM ID", "On Boot", "Memory Host %") into
    /// JSON-friendly identifier keys ("vmID", "onBoot", "memoryHost").
    /// </summary>
    private static IDictionary<string, object?> NormaliseKeyValue(IDictionary<string, object?> items)
    {
        var result = new Dictionary<string, object?>(items.Count);
        foreach (var (k, v) in items)
        {
            result[JsonKey.FromDisplay(k)] = v;
        }
        return result;
    }

    private static (string Key, object? Value) TransformByConvention(string name, object? raw, ColumnKind kind)
        => kind switch
        {
            // Flag columns are pre-formatted by the engine as "X" / "" strings — turn them back into booleans.
            ColumnKind.Flag => (name[..^4], raw is string s ? s.Length > 0 : raw is bool b && b),
            // Wrap columns are just text with multi-line content — keep value, strip the suffix from the key.
            ColumnKind.Wrap => (name[..^4], raw),
            // GB / MB columns carry raw byte counts from the engine; strip the unit suffix
            // so the JSON exposes the raw bytes under a bare key (e.g. "memorySize").
            ColumnKind.GB => (name[..^2], raw),
            ColumnKind.MB => (name[..^2], raw),
            // Pct columns carry a 0–1 fraction; strip the suffix for consistency (e.g. "cpuUsage").
            ColumnKind.Percentage => (name[..^3], raw),
            // DateOnly / DateTime / Number / Text: keep both key and value as the engine produced them.
            _ => (name, raw),
        };
}

/// <summary>Discriminated record for the kinds of block a section can carry.</summary>
internal abstract record JsonBlock
{
    public sealed record KeyValue(string Title, IDictionary<string, object?> Items) : JsonBlock;
    public sealed record Table(string? Title, IList<object?> Rows) : JsonBlock;
}
