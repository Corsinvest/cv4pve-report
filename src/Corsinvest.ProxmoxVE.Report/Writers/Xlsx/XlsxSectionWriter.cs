/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;

namespace Corsinvest.ProxmoxVE.Report.Writers.Xlsx;

/// <summary>
/// Section writer that materializes content into an Excel sheet via the
/// existing <see cref="SheetWriter"/> engine. Thin adapter that translates
/// format-agnostic ISectionWriter calls into ClosedXML.
/// </summary>
internal sealed class XlsxSectionWriter(SheetWriter inner) : ISectionWriter
{
    /// <summary>Underlying SheetWriter (exposed for migration of callers that still need direct ClosedXML access).</summary>
    public SheetWriter Inner { get; } = inner;

    public void AddBackLink(string label, string linkKey)
        => Inner.WriteBackLink(label, linkKey);

    public void AddKeyValue(string title, IDictionary<string, object?> items)
        => Inner.WriteKeyValue(title, new Dictionary<string, object?>(items));

    public void AddKeyValueRow(params (string Title, IDictionary<string, object?> Items)[] blocks)
    {
        if (blocks.Length == 0) { return; }

        // Render each block at the same starting row but at increasing column offsets,
        // mirroring the original side-by-side layout used by the detail pages.
        const int colsPerBlock = 3; // key column + value column + 1 spacer
        var startRow = Inner.Row;
        var maxRowAfter = startRow;

        for (var i = 0; i < blocks.Length; i++)
        {
            Inner.Row = startRow;
            Inner.Col = 1 + (i * colsPerBlock);
            Inner.WriteKeyValue(blocks[i].Title, new Dictionary<string, object?>(blocks[i].Items));
            if (Inner.Row > maxRowAfter) { maxRowAfter = Inner.Row; }
        }

        Inner.Row = maxRowAfter;
        Inner.Col = 1;
    }

    public ITableHandle AddTable<T>(string? title, IEnumerable<T> data, TableOptions<T>? options = null)
    {
        var dataList = data as IList<T> ?? [.. data];
        var table = Inner.CreateTable(title, dataList);

        if (options != null)
        {
            ApplyColumnLinks(table, dataList, options.ColumnLinks);
            RegisterRowKeys(table, dataList, options.RegisterRowKeys);
        }

        return new XlsxTableHandle(title ?? "", table);
    }

    public void AppendData<T>(ITableHandle table, IEnumerable<T> data)
    {
        var xlsx = (XlsxTableHandle)table;
        Inner.AppendData(xlsx.Table, data);
    }

    public void Dispose()
    {
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

        // For each row, register every key returned by the mapper against that row's address.
        // We bypass SheetWriter.RegisterRowLinks (which is column-keyed) because we need
        // arbitrary multi-key registration per row.
        var firstDataRow = table.DataRange.FirstRow().RowNumber();
        for (int i = 0; i < rows.Count; i++)
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
