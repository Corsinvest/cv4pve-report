/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;

namespace Corsinvest.ProxmoxVE.Report.Writers.Xlsx;

/// <summary>
/// Buffers all section operations and replays them at <see cref="Dispose"/> time.
/// The buffering exists so the per-sheet Index can reserve exactly N rows
/// (where N = number of titled tables) at a known offset — between the first
/// key/value block (the resource "identity" header) and the first table —
/// without using <c>InsertRowsAbove</c>, which desynchronises ClosedXML
/// table ranges and breaks already-written hyperlinks.
/// </summary>
internal sealed class XlsxSectionWriter(SheetWriter inner) : ISectionWriter
{
    private enum OpKind { BackLink, KeyValue, Table, Append }

    private readonly List<(OpKind Kind, Action Run)> _pending = [];
    private int _titledTableCount;

    public SheetWriter Inner { get; } = inner;

    public void AddBackLink(string label, string linkKey)
        => _pending.Add((OpKind.BackLink, () => Inner.WriteBackLink(label, linkKey)));

    public void AddKeyValue(string title, IDictionary<string, object?> items)
        => _pending.Add((OpKind.KeyValue, () => Inner.WriteKeyValue(title, new(items))));

    public void AddKeyValueRow(params (string Title, IDictionary<string, object?> Items)[] blocks)
    {
        if (blocks.Length == 0) { return; }

        var items = blocks.Select(b => (b.Title, Items: new Dictionary<string, object?>(b.Items)))
                          .ToArray();

        _pending.Add((OpKind.KeyValue, () =>
        {
            const int colsPerBlock = 3;
            var startRow = Inner.Row;
            var maxRowAfter = startRow;

            for (var i = 0; i < items.Length; i++)
            {
                Inner.Row = startRow;
                Inner.Col = 1 + (i * colsPerBlock);
                Inner.WriteKeyValue(items[i].Title, items[i].Items);
                if (Inner.Row > maxRowAfter) { maxRowAfter = Inner.Row; }
            }

            Inner.Row = maxRowAfter;
            Inner.Col = 1;
        }));
    }

    public ITableHandle AddTable<T>(string? title, IEnumerable<T> data, TableOptions<T>? options = null)
    {
        var dataList = data as IList<T> ?? [.. data];
        var handle = new XlsxTableHandle(title ?? "");

        if (title != null) { _titledTableCount++; }

        _pending.Add((OpKind.Table, () =>
        {
            var table = Inner.CreateTable(title, dataList);
            handle.Table = table;

            if (options != null)
            {
                ApplyColumnLinks(table, dataList, options.ColumnLinks);
                RegisterRowKeys(table, dataList, options.RegisterRowKeys);
            }
        }));

        return handle;
    }

    public void AppendData<T>(ITableHandle table, IEnumerable<T> data)
    {
        var xlsx = (XlsxTableHandle)table;
        var dataList = data as IList<T> ?? [.. data];
        _pending.Add((OpKind.Append, () =>
        {
            if (xlsx.Table != null) { Inner.AppendData(xlsx.Table, dataList); }
        }));
    }

    public void Dispose()
    {
        // Replay until we've flushed: BackLink + the first KeyValue block (the "identity" header).
        // Then reserve the index rows. Then replay the remaining ops (other KeyValues + Tables).
        // If there is no leading KeyValue, the index is reserved at the very top.
        var firstKvIndex = _pending.FindIndex(op => op.Kind == OpKind.KeyValue);
        var splitAfter = firstKvIndex < 0 ? -1 : firstKvIndex;

        for (var i = 0; i <= splitAfter; i++) { _pending[i].Run(); }

        if (_titledTableCount > 0) { Inner.ReserveIndexRows(_titledTableCount); }

        for (var i = splitAfter + 1; i < _pending.Count; i++) { _pending[i].Run(); }

        Inner.WriteIndex();
        Inner.AdjustColumns();
    }

    private void ApplyColumnLinks<T>(IXLTable table, IList<T> rows, IDictionary<string, Func<T, string?>>? columnLinks)
    {
        if (columnLinks == null) { return; }

        foreach (var (colName, mapper) in columnLinks)
        {
            var firstDataRow = table.DataRange.FirstRow().RowNumber();
            Inner.ApplyColumnLinks(table, colName, cell =>
            {
                var idx = cell.Address.RowNumber - firstDataRow;
                return idx >= 0 && idx < rows.Count ? mapper(rows[idx]) : null;
            });
        }
    }

    private void RegisterRowKeys<T>(IXLTable table, IList<T> rows, Func<T, IEnumerable<string>>? rowKeys)
    {
        if (rowKeys == null) { return; }

        var firstDataRow = table.DataRange.FirstRow().RowNumber();
        for (var i = 0; i < rows.Count; i++)
        {
            var rowNumber = firstDataRow + i;
            foreach (var key in rowKeys(rows[i]))
            {
                if (string.IsNullOrWhiteSpace(key)) { continue; }
                Inner.RegisterDirectLink(key, rowNumber);
            }
        }
    }
}
