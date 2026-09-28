/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Writers.Json;

namespace Corsinvest.ProxmoxVE.Report.Tests.Writers.Json;

public class JsonSectionWriterTests
{
    private static IDictionary<string, object?> FirstRow<T>(T row)
    {
        var writer = new JsonSectionWriter("Test");
        writer.AddTable(null, [row]);
        var table = Assert.IsType<JsonBlock.Table>(Assert.Single(writer.Blocks));
        return Assert.IsAssignableFrom<IDictionary<string, object?>>(Assert.Single(table.Rows));
    }

    [Fact]
    public void AddTable_SizeAndPercentageSameStem_KeepBoth()
    {
        var row = FirstRow(new { MemoryUsageGB = 2048L, MemoryUsagePct = 0.5 });

        Assert.Equal(2048L, row["memoryUsageBytes"]);
        Assert.Equal(0.5, row["memoryUsage"]);
        Assert.False(row.ContainsKey("memoryUsageGB"));
    }

    [Fact]
    public void AddTable_SizeWithoutCollision_KeepsStrippedKey()
    {
        var row = FirstRow(new { MemoryGB = 1024L, CpuUsagePct = 0.25 });

        Assert.Equal(1024L, row["memory"]);
        Assert.Equal(0.25, row["cpuUsage"]);
    }

    [Fact]
    public void AddTable_AcronymProperty_IsLowerCased()
    {
        var row = FirstRow(new { IP = "10.0.0.1" });

        Assert.Equal("10.0.0.1", row["ip"]);
    }
}
