/*
using DocumentFormat.OpenXml.Spreadsheet;
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using System.Text.RegularExpressions;

namespace Corsinvest.ProxmoxVE.Report;

internal partial class SheetWriter(IXLWorksheet ws, Dictionary<string, string> sheetLinks)
{
    private readonly List<(string Title, int Row)> _tableIndex = [];
    private int _indexStartRow;

    public int Row { get; set; } = 1;
    public int Col { get; set; } = 1;

    [GeneratedRegex("(?<=[a-z])([A-Z])|(?<=[A-Z])([A-Z][a-z])")]
    private static partial Regex PascalCaseSplitRegex();

    private static string PascalCaseToWords(string name)
        => PascalCaseSplitRegex().Replace(name, " $1$2").Trim();

    private static bool IsGB(string name) => name.EndsWith("GB", StringComparison.OrdinalIgnoreCase);
    private static bool IsMB(string name) => name.EndsWith("MB", StringComparison.OrdinalIgnoreCase);
    private static bool IsPct(string name) => name.EndsWith("Pct", StringComparison.OrdinalIgnoreCase);
    private static bool IsWrap(string name) => name.EndsWith("Wrap", StringComparison.OrdinalIgnoreCase);
    private static bool IsDateOnly(string name) => name.EndsWith("Date", StringComparison.OrdinalIgnoreCase);
    private static bool IsFlag(string name) => name.EndsWith("Flag", StringComparison.OrdinalIgnoreCase);

    public void AdjustColumns() => ws.Columns().AdjustToContents();

    public void WriteBackLink(string label, string linkKey)
    {
        if (!sheetLinks.TryGetValue(linkKey, out var target)) { return; }
        var cell = ws.Cell(1, 2);
        cell.Value = $"← {label}";
        cell.SetHyperlink(new XLHyperlink($"'{target}'!A1"));
        cell.Style.Font.SetFontColor(XLColor.Blue);
        cell.Style.Font.SetUnderline(XLFontUnderlineValues.Single);
        cell.Style.Font.SetItalic(true);
    }

    /// <summary>Writes a key-value block starting at current Row/Col and advances Row.</summary>
    public void WriteKeyValue(string title, Dictionary<string, object?> items)
    {
        var startRow = Row;
        var col = Col;

        ws.Cell(Row, col).Value = title;
        ws.Cell(Row, col).Style.Font.SetBold(true);
        ws.Cell(Row, col).Style.Font.SetFontSize(12);
        Row++;

        foreach (var (key, value) in items)
        {
            ws.Cell(Row, col).Value = key;
            ws.Cell(Row, col).Style.Font.SetBold(true);
            var valueCell = ws.Cell(Row, col + 1);
            var strValue = value?.ToString() ?? "";
            valueCell.Value = value switch
            {
                bool b => b ? "X" : "",
                double or float or int or long => Convert.ToDouble(value),
                _ => strValue
            };

            if (value is bool)
            {
                valueCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            else if (key.Equals("Node", StringComparison.OrdinalIgnoreCase) && value is string nodeName)
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
        if (_indexStartRow == 0) { return; }
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

    public void CreateOrAddTable<T>(ref IXLTable? table, string? title, IEnumerable<T> data, Action<IXLTable>? configure = null)
    {
        if (table == null)
        {
            table = CreateTable(title, data.ToList(), configure);
        }
        else
        {
            AppendData(table, data.ToList(), configure);
        }
    }

    /// <summary>Creates a table at current Row/Col, registers it in the index, and advances Row.</summary>
    public IXLTable CreateTable<T>(string? title, IEnumerable<T> data, Action<IXLTable>? configure = null)
    {
        if (title != null)
        {
            _tableIndex.Add((title, Row));
            ws.Cell(Row, Col).Value = title;
            ws.Cell(Row, Col).Style.Font.SetBold(true);
            Row++;
        }

        var table = ws.Cell(Row, Col).InsertTable(data, true);
        table.AutoFilter.IsEnabled = true;

        foreach (var col in table.Fields)
        {
            var dataCol = table.DataRange.Column(col.Index + 1);

            if (IsPct(col.Name))
            {
                col.HeaderCell.Value = PascalCaseToWords(col.Name[..^"Pct".Length]) + " %";
                dataCol.Style.NumberFormat.Format = "0.00%";
            }
            else if (IsGB(col.Name) || IsMB(col.Name))
            {
                col.HeaderCell.Value = PascalCaseToWords(col.Name);
                dataCol.Style.NumberFormat.Format = "#,##0.00";
            }
            else if (IsWrap(col.Name))
            {
                col.HeaderCell.Value = PascalCaseToWords(col.Name[..^"Wrap".Length]);
                table.Worksheet.Column(dataCol.FirstCell().Address.ColumnNumber).Width = 40;
                dataCol.Style.Alignment.WrapText = true;
            }
            else if (IsFlag(col.Name))
            {
                col.HeaderCell.Value = PascalCaseToWords(col.Name[..^"Flag".Length]);
                dataCol.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            else
            {
                col.HeaderCell.Value = PascalCaseToWords(col.Name);
            }

            if (dataCol.FirstCell().Value.IsDateTime)
            {
                dataCol.Style.NumberFormat.Format = IsDateOnly(col.Name)
                    ? "dd/MM/yyyy"
                    : "dd/MM/yyyy HH:mm:ss";
            }
        }

        configure?.Invoke(table);
        Row += table.RowCount() + 2;
        return table;
    }

    /// <summary>Appends data to an existing table. Column formats are inherited; only booleans need fixing.</summary>
    public void AppendData<T>(IXLTable table, IEnumerable<T> data, Action<IXLTable>? configure = null)
    {
        var beforeCount = table.RowCount();
        table.AppendData(data);
        var afterCount = table.RowCount();

        if (afterCount <= beforeCount) { return; }

        configure?.Invoke(table);
        Row += afterCount - beforeCount;
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
            {
                sheetLinks[key] = $"{ws.Name}!A{cell.Address.RowNumber}";
            }
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
        ApplyNodeLinks(table);
        ApplyVmIdLinks(table);
        ApplyColumnLinks(table, "Source", cell => $"node:{cell.Value}");
        ApplyColumnLinks(table, "Target", cell => $"node:{cell.Value}");
    }

    public void ApplyStorageLinks(IXLTable table)
        => ApplyColumnLinks(table,
                            "Storage",
                            cell => string.IsNullOrWhiteSpace(cell.Value.ToString())
                                    ? null
                                    : "storage:link");


    public void RegisterNetworkLinks(IXLTable table, string node)
        => RegisterRowLinks(table, "Interface", cell => $"node:{node}:network:{cell.Value}");
}
