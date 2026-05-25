/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;

namespace Corsinvest.ProxmoxVE.Report.Writers.Xlsx;

internal sealed class SheetWriter(IXLWorksheet ws, Dictionary<string, string> sheetLinks)
{
    private readonly List<(string Title, int Row)> _tableIndex = [];
    private int _indexStartRow;

    public int Row { get; set; } = 1;
    public int Col { get; set; } = 1;

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

    public void WriteKeyValue(string title, Dictionary<string, object?> items)
    {
        var startRow = Row;
        var col = Col;

        ws.Cell(Row, col).Value = title;
        ws.Cell(Row, col).Style.Font.SetBold(true);
        ws.Cell(Row, col).Style.Font.SetFontSize(12);
        Row++;

        foreach (var (rawKey, value) in items)
        {
            var (kind, displayLabel) = ColumnConvention.Parse(rawKey);
            var labelCell = ws.Cell(Row, col);
            labelCell.Value = displayLabel;
            labelCell.Style.Font.SetBold(true);
            var valueCell = ws.Cell(Row, col + 1);
            var convertedValue = ConvertKeyValue(value, kind);
            var strValue = convertedValue?.ToString() ?? "";
            valueCell.Value = convertedValue switch
            {
                bool b => b ? "X" : "",
                double or float or int or long => Convert.ToDouble(convertedValue),
                _ => strValue
            };

            switch (kind)
            {
                case ColumnKind.Percentage:
                    valueCell.Style.NumberFormat.Format = "0.00%";
                    break;
                case ColumnKind.GB:
                case ColumnKind.MB:
                    valueCell.Style.NumberFormat.Format = "#,##0.00";
                    break;
            }

            if (convertedValue is bool)
            {
                valueCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            else if (rawKey.Equals("Node", StringComparison.OrdinalIgnoreCase) && value is string nodeName)
            {
                SetHyperlink(valueCell, LinkKey.Node(nodeName));
            }
            Row++;
        }

        var border = ws.Range(startRow, col, Row - 1, col + 1);
        border.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        border.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        Row++;
    }

    public void ReserveIndexRows(int tableCount)
    {
        if (tableCount == 0) { return; }
        _indexStartRow = Row;
        Row += IndexRowsFor(tableCount);
    }

    public static int IndexRowsFor(int tableCount) => tableCount == 0 ? 0 : ((tableCount + 1) / 2) + 2;

    public void WriteIndex()
    {
        if (_indexStartRow == 0 || _tableIndex.Count == 0) { return; }
        var r = _indexStartRow;
        var c = Col;
        ws.Cell(r, c).Value = "Index";
        ws.Cell(r, c).Style.Font.SetBold(true);
        ws.Cell(r, c).Style.Font.SetFontSize(12);
        r++;

        var rowsPerColumn = (_tableIndex.Count + 1) / 2;
        for (var i = 0; i < _tableIndex.Count; i++)
        {
            var (tblTitle, tblRow) = _tableIndex[i];
            var cellRow = r + (i % rowsPerColumn);
            var cellCol = c + (i / rowsPerColumn);
            var cell = ws.Cell(cellRow, cellCol);
            cell.Value = tblTitle;
            cell.Style.Font.SetUnderline(XLFontUnderlineValues.Single);
            cell.Style.Font.SetFontColor(XLColor.Blue);
            cell.SetHyperlink(new XLHyperlink($"'{ws.Name}'!A{tblRow}"));
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
            var (kind, displayName) = ColumnConvention.Parse(col.Name);
            col.HeaderCell.Value = displayName;

            switch (kind)
            {
                case ColumnKind.Percentage:
                    dataCol.Style.NumberFormat.Format = "0.00%";
                    break;

                case ColumnKind.GB:
                    ConvertBytesToUnit(dataCol, UnitFormat.BytesToGB);
                    dataCol.Style.NumberFormat.Format = "#,##0.00";
                    break;

                case ColumnKind.MB:
                    ConvertBytesToUnit(dataCol, UnitFormat.BytesToMB);
                    dataCol.Style.NumberFormat.Format = "#,##0.00";
                    break;

                case ColumnKind.Wrap:
                    table.Worksheet.Column(dataCol.FirstCell().Address.ColumnNumber).Width = 40;
                    dataCol.Style.Alignment.WrapText = true;
                    break;

                case ColumnKind.Flag:
                    dataCol.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    break;

                case ColumnKind.HealthScore:
                    dataCol.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    dataCol.Style.NumberFormat.Format = "0";
                    dataCol.AddConditionalFormat().ColorScale()
                           .LowestValue(XLColor.FromHtml("#d05a5a"))                            // red ~ critical
                           .Midpoint(XLCFContentType.Number, 60, XLColor.FromHtml("#f0d264"))   // yellow ~ warn
                           .HighestValue(XLColor.FromHtml("#7cc77c"));                          // green ~ good
                    break;

                case ColumnKind.StatusBadge:
                    dataCol.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    dataCol.Style.Font.SetBold(true);
                    dataCol.AddConditionalFormat().WhenEquals("\"PASS\"").Fill.SetBackgroundColor(XLColor.FromHtml("#d4edda"));
                    dataCol.AddConditionalFormat().WhenEquals("\"FAIL\"").Fill.SetBackgroundColor(XLColor.FromHtml("#f8d7da"));
                    dataCol.AddConditionalFormat().WhenEquals("\"PARTIAL\"").Fill.SetBackgroundColor(XLColor.FromHtml("#fff3cd"));
                    dataCol.AddConditionalFormat().WhenEquals("\"N/A\"").Fill.SetBackgroundColor(XLColor.FromHtml("#e9ecef"));
                    break;

                case ColumnKind.ScoreBadge:
                    dataCol.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    dataCol.Style.NumberFormat.Format = "0%";
                    dataCol.AddConditionalFormat().ColorScale()
                           .LowestValue(XLColor.FromHtml("#d05a5a"))
                           .Midpoint(XLCFContentType.Number, 0.6, XLColor.FromHtml("#f0d264"))
                           .HighestValue(XLColor.FromHtml("#7cc77c"));
                    break;
            }

            if (dataCol.FirstCell().Value.IsDateTime)
            {
                dataCol.Style.NumberFormat.Format = kind == ColumnKind.DateOnly
                                                        ? "dd/MM/yyyy"
                                                        : "dd/MM/yyyy HH:mm:ss";
            }
        }

        configure?.Invoke(table);
        Row += table.RowCount() + 2;
        return table;
    }

    public void AppendData<T>(IXLTable table, IEnumerable<T> data, Action<IXLTable>? configure = null)
    {
        var beforeCount = table.RowCount();
        table.AppendData(data);
        var afterCount = table.RowCount();

        if (afterCount <= beforeCount) { return; }

        configure?.Invoke(table);
        Row += afterCount - beforeCount;
    }

    private static void ConvertBytesToUnit(IXLRangeColumn dataCol, Func<double, double> convert)
    {
        foreach (var cell in dataCol.Cells())
        {
            if (cell.Value.IsNumber)
            {
                cell.Value = convert(cell.Value.GetNumber());
            }
        }
    }

    private static object? ConvertKeyValue(object? value, ColumnKind kind)
        => value is IConvertible c && (kind == ColumnKind.GB || kind == ColumnKind.MB)
            ? kind == ColumnKind.GB
                ? UnitFormat.BytesToGB(c.ToDouble(System.Globalization.CultureInfo.InvariantCulture))
                : UnitFormat.BytesToMB(c.ToDouble(System.Globalization.CultureInfo.InvariantCulture))
            : value;

    private void SetHyperlink(IXLCell cell, string linkKey)
    {
        if (!sheetLinks.TryGetValue(linkKey, out var target)) { return; }
        // target is either "SheetName" (link to A1) or "SheetName!Arow" (link to specific row)
        var href = target.Contains('!') ? $"'{target}'" : $"'{target}'!A1";
        cell.SetHyperlink(new XLHyperlink(href));
    }

    public void RegisterDirectLink(string linkKey, int rowNumber)
        => sheetLinks[linkKey] = $"{ws.Name}!A{rowNumber}";

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
                                                    || f.HeaderCell.Value.ToString().Equals(ColumnConvention.PascalCaseToWords(colName), StringComparison.OrdinalIgnoreCase));
        if (col == null) { return; }

        foreach (var cell in table.DataRange.Column(col.Index + 1).Cells())
        {
            var key = getKey(cell);
            if (!string.IsNullOrWhiteSpace(key)) { SetHyperlink(cell, key); }
        }
    }

    public void ApplyNodeLinks(IXLTable table)
        => ApplyColumnLinks(table, "Node", cell => LinkKey.Node(cell.Value.ToString()));

    public void ApplyVmIdLinks(IXLTable table)
        => ApplyColumnLinks(table, "VmId", cell =>
        {
            var id = cell.Value.IsNumber
                    ? (long)cell.Value.GetNumber()
                    : long.TryParse(cell.Value.ToString(), out var sid)
                        ? sid
                        : 0;

            if (id > 0) { cell.Value = id; }

            return id > 0
                    ? LinkKey.Vm(id)
                    : null;
        });

    public void ApplyReplicationLinks(IXLTable table)
    {
        ApplyNodeLinks(table);
        ApplyVmIdLinks(table);
        ApplyColumnLinks(table, "Source", cell => LinkKey.Node(cell.Value.ToString()));
        ApplyColumnLinks(table, "Target", cell => LinkKey.Node(cell.Value.ToString()));
    }

    public void ApplyStorageLinks(IXLTable table)
        => ApplyColumnLinks(table,
                            "Storage",
                            cell => string.IsNullOrWhiteSpace(cell.Value.ToString())
                                    ? null
                                    : LinkKey.Storages);

    public void RegisterNetworkLinks(IXLTable table, string node)
        => RegisterRowLinks(table, "Interface", cell => LinkKey.NodeNetwork(node, cell.Value.ToString()));
}
