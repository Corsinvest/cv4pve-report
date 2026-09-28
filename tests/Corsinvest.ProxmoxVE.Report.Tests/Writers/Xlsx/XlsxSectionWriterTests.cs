/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Report.Writers;
using Corsinvest.ProxmoxVE.Report.Writers.Xlsx;

namespace Corsinvest.ProxmoxVE.Report.Tests.Writers.Xlsx;

public class XlsxSectionWriterTests
{
    [Fact]
    public void AddTable_HiddenColumn_IsRemovedAndLinksStillApply()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Issues");
        workbook.Worksheets.Add("Nodes");
        var links = new Dictionary<string, string> { ["section:nodes"] = "Nodes" };

        var rows = new[]
        {
            new { Severity = "Warning", Section = "Nodes", LinkKey = "section:nodes", MessageWrap = "msg" },
        };

        var writer = new XlsxSectionWriter(new SheetWriter(ws, links));
        var handle = writer.AddTable(null,
                                     rows,
                                     new TableOptions<object>().WithColumnLink("Section", r => (string?)((dynamic)r).LinkKey)
                                                               .WithHiddenColumns("LinkKey"));
        writer.Dispose();

        var table = ws.Tables.Single();
        Assert.Equal(["Severity", "Section", "Message"], table.Fields.Select(f => f.HeaderCell.GetString()));
        Assert.Equal("msg", table.DataRange.Row(1).Cell(3).GetString());
        Assert.True(table.DataRange.Row(1).Cell(2).HasHyperlink);
        Assert.Throws<NotSupportedException>(() => writer.AppendData(handle, rows));
    }

    [Fact]
    public void AddTable_DateSuffixOnDateTime_UsesDateTimeFormat()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Log");

        var writer = new XlsxSectionWriter(new SheetWriter(ws, []));
        writer.AddTable(null, new[] { new { TimeDate = new DateTime(2026, 1, 2, 13, 45, 10) } });
        writer.Dispose();

        var cell = ws.Tables.Single().DataRange.Row(1).Cell(1);
        Assert.Equal("dd/MM/yyyy HH:mm:ss", cell.Style.NumberFormat.Format);
    }
}
