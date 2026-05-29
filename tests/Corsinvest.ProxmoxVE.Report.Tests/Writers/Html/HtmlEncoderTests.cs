/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Writers.Html;

namespace Corsinvest.ProxmoxVE.Report.Tests.Writers.Html;

public class HtmlEncoderTests
{
    [Theory]
    [InlineData("Cluster", "cluster.html")]
    [InlineData("Storages", "storages.html")]
    [InlineData("Cluster Access", "cluster-access.html")]
    [InlineData("RRD Nodes", "rrd-nodes.html")]
    public void PageFileName_TopLevelSections_GoToRoot(string sectionName, string expected)
    {
        Assert.Equal(expected, HtmlEncoder.PageFileName(sectionName));
    }

    [Theory]
    [InlineData("Node cc01", "nodes/cc01.html")]
    [InlineData("Node pve-host.example.com", "nodes/pve-host-example-com.html")]
    [InlineData("VM 100", "vms/100.html")]
    [InlineData("CT 200", "containers/200.html")]
    public void PageFileName_DetailSections_GoToSubfolders(string sectionName, string expected)
    {
        Assert.Equal(expected, HtmlEncoder.PageFileName(sectionName));
    }

    [Theory]
    [InlineData("Cluster", 0)]
    [InlineData("Storages", 0)]
    [InlineData("Node cc01", 1)]
    [InlineData("VM 100", 1)]
    [InlineData("CT 200", 1)]
    public void PageDepth_CountsSubdirectorySegments(string sectionName, int expected)
    {
        Assert.Equal(expected, HtmlEncoder.PageDepth(sectionName));
    }

    [Theory]
    [InlineData("plain text", "plain text")]
    [InlineData("a & b", "a &amp; b")]
    [InlineData("<script>", "&lt;script&gt;")]
    [InlineData("name=\"value\"", "name=&quot;value&quot;")]
    [InlineData("it's", "it&#39;s")]
    public void Text_EscapesHtmlEntities(string input, string expected)
    {
        Assert.Equal(expected, HtmlEncoder.Text(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Text_NullOrEmpty_ReturnsEmpty(string? input)
    {
        Assert.Equal("", HtmlEncoder.Text(input));
    }

    [Fact]
    public void PageHref_EncodesPathForAttribute()
    {
        // Path itself doesn't need extra encoding here, but the helper should round-trip through Attr.
        Assert.Equal("nodes/cc01.html", HtmlEncoder.PageHref("Node cc01"));
    }
}
