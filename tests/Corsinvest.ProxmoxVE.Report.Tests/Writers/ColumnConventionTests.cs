/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report.Tests.Writers;

public class ColumnConventionTests
{
    [Theory]
    [InlineData("CpuUsagePct", ColumnKind.Percentage, "Cpu Usage %")]
    [InlineData("MemoryUsageGB", ColumnKind.GB, "Memory Usage GB")]
    [InlineData("DiskUsageMB", ColumnKind.MB, "Disk Usage MB")]
    [InlineData("DescriptionWrap", ColumnKind.Wrap, "Description")]
    [InlineData("EnableFlag", ColumnKind.Flag, "Enable")]
    [InlineData("StartDate", ColumnKind.DateOnly, "Start Date")]
    public void Parse_RecognisesSuffixConventions(string input, object expectedKind, string expectedLabel)
    {
        var (kind, label) = ColumnConvention.Parse(input);

        Assert.Equal((ColumnKind)expectedKind, kind);
        Assert.Equal(expectedLabel, label);
    }

    [Fact]
    public void Parse_HealthScore_StripsScoreFromLabel()
    {
        var (kind, label) = ColumnConvention.Parse("CpuHealthScore");

        Assert.Equal(ColumnKind.HealthScore, kind);
        Assert.Equal("Cpu Health", label);
    }

    [Theory]
    [InlineData("PlainText")]
    [InlineData("NodeName")]
    public void Parse_PascalCaseWithoutSuffix_IsTextWithWords(string input)
    {
        var (kind, label) = ColumnConvention.Parse(input);

        Assert.Equal(ColumnKind.Text, kind);
        Assert.Equal(ColumnConvention.PascalCaseToWords(input), label);
    }

    [Theory]
    [InlineData("VM ID")]
    [InlineData("CPU Usage %")]
    [InlineData("vCPUs")]
    public void Parse_DisplayLabel_IsKeptVerbatim(string input)
    {
        var (_, label) = ColumnConvention.Parse(input);

        Assert.Equal(input, label);
    }

    [Fact]
    public void Parse_HumanGBLabel_StaysGBKind()
    {
        var (kind, label) = ColumnConvention.Parse("Memory GB");

        Assert.Equal(ColumnKind.GB, kind);
        Assert.Equal("Memory GB", label);
    }

    [Fact]
    public void PascalCaseToWords_SplitsOnUpperCase()
    {
        Assert.Equal("Cpu Usage Percentage", ColumnConvention.PascalCaseToWords("CpuUsagePercentage"));
        Assert.Equal("VM Id", ColumnConvention.PascalCaseToWords("VMId"));
    }

    [Fact]
    public void Parse_PropertyInfo_FallbackToCLRType_Numeric()
    {
        var prop = typeof(SampleRow).GetProperty(nameof(SampleRow.Count))!;

        var (kind, label) = ColumnConvention.Parse(prop);

        Assert.Equal(ColumnKind.Number, kind);
        Assert.Equal("Count", label);
    }

    [Fact]
    public void Parse_PropertyInfo_FallbackToCLRType_DateTime()
    {
        var prop = typeof(SampleRow).GetProperty(nameof(SampleRow.Created))!;

        var (kind, label) = ColumnConvention.Parse(prop);

        Assert.Equal(ColumnKind.DateTime, kind);
        Assert.Equal("Created", label);
    }

    [Fact]
    public void Parse_PropertyInfo_SuffixWinsOverCLRType()
    {
        // long is numeric, but the "GB" suffix must take precedence so the writer formats it as GB.
        var prop = typeof(SampleRow).GetProperty(nameof(SampleRow.MemoryGB))!;

        var (kind, _) = ColumnConvention.Parse(prop);

        Assert.Equal(ColumnKind.GB, kind);
    }

    [Theory]
    [InlineData(nameof(SampleRow.TimeDate), "Time Date")]
    [InlineData(nameof(SampleRow.CreationDate), "Creation Date")]
    [InlineData(nameof(SampleRow.SnapshotDate), "Snapshot Date")]
    public void Parse_PropertyInfo_DateSuffixOnDateTime_KeepsTime(string propertyName, string expectedLabel)
    {
        var prop = typeof(SampleRow).GetProperty(propertyName)!;

        var (kind, label) = ColumnConvention.Parse(prop);

        Assert.Equal(ColumnKind.DateTime, kind);
        Assert.Equal(expectedLabel, label);
    }

    [Fact]
    public void Parse_PropertyInfo_DateSuffixOnString_StaysDateOnly()
    {
        var prop = typeof(SampleRow).GetProperty(nameof(SampleRow.LabelDate))!;

        var (kind, _) = ColumnConvention.Parse(prop);

        Assert.Equal(ColumnKind.DateOnly, kind);
    }

    private sealed class SampleRow
    {
        public int Count { get; set; }
        public DateTime Created { get; set; }
        public long MemoryGB { get; set; }
        public DateTime TimeDate { get; set; }
        public DateTime? CreationDate { get; set; }
        public DateTimeOffset SnapshotDate { get; set; }
        public string LabelDate { get; set; } = "";
    }
}
