/*
using DocumentFormat.OpenXml.Spreadsheet;
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace Corsinvest.ProxmoxVE.Report;

internal partial class SheetWriter(IXLWorksheet ws, Dictionary<string, string> sheetLinks)
{
    private readonly List<(string Title, int Row)> _tableIndex = [];
    private int _indexStartRow;

    public int Row { get; set; } = 1;
    public int Col { get; set; } = 1;
    public bool SkipEmptyCollections { get; set; }
    public string SheetName => ws.Name;
    public IXLWorksheet Worksheet => ws;

    private static readonly HashSet<string> WrapColumnNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Description",
        "Notes",
        "Comments",
        "Tags",
        "AllowedTags",
        "Title",
        "Content"
    };

    [GeneratedRegex("(?<=[a-z])([A-Z])|(?<=[A-Z])([A-Z][a-z])")]
    private static partial Regex PascalCaseSplitRegex();

    private static string PascalCaseToWords(string name)
        => PascalCaseSplitRegex().Replace(name, " $1$2").Trim();

    public void AdjustColumns() => ws.Columns().AdjustToContents();

    /// <summary>Writes a key-value block starting at current Row/Col and advances Row.</summary>
    public void WriteKeyValue(string title, Dictionary<string, object?> items)
    {
        if (SkipEmptyCollections && items.Count == 0) { return; }

        var startRow = Row;
        var col = Col;

        ws.Cell(Row, col).Value = title;
        ws.Cell(Row, col).Style.Font.SetBold(true);
        ws.Cell(Row, col).Style.Font.SetFontSize(12);
        ws.Range(Row, col, Row, col + 1).Merge();
        Row++;

        foreach (var (key, value) in items)
        {
            ws.Cell(Row, col).Value = key;
            ws.Cell(Row, col).Style.Font.SetBold(true);
            var valueCell = ws.Cell(Row, col + 1);
            valueCell.Value = value?.ToString() ?? string.Empty;
            if (key.Equals("Node", StringComparison.OrdinalIgnoreCase) && value is string nodeName)
            {
                SetHyperlink(valueCell, $"node:{nodeName}");
            }
            Row++;
        }

        var border = ws.Range(startRow, col, Row - 1, col + 1);
        border.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        border.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        Row++; // empty row
    }

    /// <summary>Reserves rows for the index (saved internally) and advances Row.</summary>
    public void ReserveIndexRows(int tableCount)
    {
        _indexStartRow = Row;
        Row += tableCount + 2;
    }

    /// <summary>Writes the index at the previously reserved rows.</summary>
    public void WriteIndex()
    {
        var r = _indexStartRow;
        var c = Col;
        ws.Cell(r, c).Value = "Index";
        ws.Cell(r, c).Style.Font.SetBold(true);
        ws.Cell(r, c).Style.Font.SetFontSize(12);
        r++;
        foreach (var (tblTitle, tblRow) in _tableIndex)
        {
            ws.Cell(r, c).Value = tblTitle;
            ws.Cell(r, c).Style.Font.SetUnderline(XLFontUnderlineValues.Single);
            ws.Cell(r, c).Style.Font.SetFontColor(XLColor.Blue);
            ws.Cell(r, c).SetHyperlink(new XLHyperlink($"'{ws.Name}'!A{tblRow}"));
            r++;
        }
    }

    /// <summary>Creates a table at current Row/Col, registers it in the index, and advances Row.</summary>
    public IXLTable CreateTable<T>(string title, IEnumerable<T> data, Action<IXLTable>? configure = null)
    {
        if (SkipEmptyCollections && !data.Any()) { return null!; }

        _tableIndex.Add((title, Row));
        ws.Cell(Row, Col).Value = title;
        ws.Cell(Row, Col).Style.Font.SetBold(true);
        Row++;

        var table = ws.Cell(Row, Col).InsertTable(data, true);
        table.AutoFilter.IsEnabled = true;

        foreach (var col in table.Fields)
        {
            if (col.Name.EndsWith("Percentage", StringComparison.OrdinalIgnoreCase))
            {
                col.HeaderCell.Value = PascalCaseToWords(col.Name[..^"Percentage".Length]) + " %";
                table.DataRange.Column(col.Index + 1).Style.NumberFormat.Format = "0.00%";
            }
            else if (col.Name.EndsWith("GB", StringComparison.OrdinalIgnoreCase) ||
                     col.Name.EndsWith("MB", StringComparison.OrdinalIgnoreCase))
            {
                col.HeaderCell.Value = PascalCaseToWords(col.Name);
                table.DataRange.Column(col.Index + 1).Style.NumberFormat.Format = "#,##0.00";
            }
            else
            {
                col.HeaderCell.Value = PascalCaseToWords(col.Name);
            }

            var dataCol = table.DataRange.Column(col.Index + 1);
            if (WrapColumnNames.Contains(col.Name))
            {
                table.Worksheet.Column(dataCol.FirstCell().Address.ColumnNumber).Width = 40;
                dataCol.Style.Alignment.WrapText = true;
            }

            var firstCell = dataCol.FirstCell();
            if (firstCell.Value.IsDateTime) { dataCol.Style.NumberFormat.Format = "dd/MM/yyyy HH:mm:ss"; }

            foreach (var cell in dataCol.Cells())
            {
                if (cell.Value.IsBoolean)
                {
                    cell.Value = cell.Value.GetBoolean() ? "X" : string.Empty;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
            }
        }

        configure?.Invoke(table);
        Row += table.RowCount() + 2;
        return table;
    }

    private void SetHyperlink(IXLCell cell, string linkKey)
    {
        if (!sheetLinks.TryGetValue(linkKey, out var target)) { return; }
        // target is either "SheetName" (link to A1) or "SheetName!Arow" (link to specific row)
        var href = target.Contains('!') ? $"'{target}'" : $"'{target}'!A1";
        cell.SetHyperlink(new XLHyperlink(href));
    }

    /// <summary>Registers per-row links for each cell in a column so other sheets can link directly to that row.</summary>
    public void RegisterRowLinks(IXLTable table, string colName, Func<IXLCell, string?> getKey)
    {
        var col = table.Fields.FirstOrDefault(f =>
            f.Name.Equals(colName, StringComparison.OrdinalIgnoreCase) ||
            f.HeaderCell.Value.ToString().Equals(colName, StringComparison.OrdinalIgnoreCase));
        if (col == null) { return; }
        foreach (var cell in table.DataRange.Column(col.Index + 1).Cells())
        {
            var key = getKey(cell);
            if (!string.IsNullOrWhiteSpace(key))
                sheetLinks[key] = $"{ws.Name}!A{cell.Address.RowNumber}";
        }
    }

    public void ApplyColumnLinks(IXLTable table, string colName, Func<IXLCell, string?> getKey)
    {
        var col = table.Fields.FirstOrDefault(f => f.Name.Equals(colName, StringComparison.OrdinalIgnoreCase)
                                                    || f.HeaderCell.Value.ToString().Equals(colName, StringComparison.OrdinalIgnoreCase)
                                                    || f.HeaderCell.Value.ToString().Equals(PascalCaseToWords(colName), StringComparison.OrdinalIgnoreCase));
        if (col == null) { return; }

        foreach (var cell in table.DataRange.Column(col.Index + 1).Cells())
        {
            var key = getKey(cell);
            if (!string.IsNullOrWhiteSpace(key)) { SetHyperlink(cell, key); }
        }
    }

    public void ApplyNodeLinks(IXLTable table)
        => ApplyColumnLinks(table, "Node", cell => $"node:{cell.Value}");

    public void ApplyVmIdLinks(IXLTable table)
        => ApplyColumnLinks(table, "VmId", cell =>
        {
            var id = cell.Value.IsNumber
                    ? (long)cell.Value.GetNumber()
                    : long.TryParse(cell.Value.ToString(), out var sid)
                        ? sid
                        : 0;
            if (id > 0)
            {
                cell.Value = id;
            }

            return id > 0
                    ? $"vm:{id}"
                    : null;
        });

    public void ApplyReplicationLinks(IXLTable table)
    {
        ApplyColumnLinks(table, "Guest", cell => $"vm:{cell.Value}");
        ApplyColumnLinks(table, "Source", cell => $"node:{cell.Value}");
        ApplyColumnLinks(table, "Target", cell => $"node:{cell.Value}");
    }

    public void ApplyStorageLinks(IXLTable table, string node)
        => ApplyColumnLinks(table, "Storage", cell => $"storage:{node}:{cell.Value}");

    public void ApplyStorageLinks(IXLTable table)
        => ApplyColumnLinks(table, "Storage", cell =>
        {
            var storage = cell.Value.ToString();
            if (string.IsNullOrWhiteSpace(storage)) { return null; }
            var nodeCol = table.Fields.FirstOrDefault(f => f.Name == "Node");
            var node = nodeCol != null
                ? table.DataRange.Column(nodeCol.Index + 1).Cell(cell.Address.RowNumber - table.DataRange.FirstRow().RowNumber() + 1).Value.ToString()
                : string.Empty;
            return $"storage:{node}:{storage}";
        });

    public void RegisterNetworkLinks(IXLTable table, string node)
        => RegisterRowLinks(table, "Interface", cell => $"node:{node}:network:{cell.Value}");

}
