/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text;
using Corsinvest.ProxmoxVE.Report.Writers.Html.Blocks;

namespace Corsinvest.ProxmoxVE.Report.Tests.Writers.Html;

public class TableBlockTests
{
    [Fact]
    public void Render_HiddenColumn_IsNotRendered()
    {
        List<object> rows = [new { Section = "Nodes", LinkKey = "section:nodes" }];
        var block = new TableBlock<object>(null, rows, new HashSet<string> { "LinkKey" });

        var sb = new StringBuilder();
        block.Render(sb, []);
        var html = sb.ToString();

        Assert.Contains("Section", html);
        Assert.DoesNotContain("Link Key", html);
        Assert.DoesNotContain("section:nodes", html);
    }

    [Fact]
    public void Render_DateSuffixOnDateTime_KeepsTime()
    {
        List<object> rows = [new { TimeDate = new DateTime(2026, 1, 2, 13, 45, 10) }];
        var block = new TableBlock<object>(null, rows);

        var sb = new StringBuilder();
        block.Render(sb, []);

        Assert.Contains("2026-01-02 13:45:10", sb.ToString());
    }
}
