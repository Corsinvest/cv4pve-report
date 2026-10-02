/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;

namespace Corsinvest.ProxmoxVE.Report.Tests;

public class CapacityPlanningTests
{
    private const long Day = 86400;
    private const long GB = 1024L * 1024 * 1024;

    private static VmRrdData Sample(double cpu, ulong memory, long netIn = 0, long netOut = 0, ulong memorySize = 8 * (ulong)GB)
        => new()
        {
            CpuUsagePercentage = cpu,
            MemoryUsage = memory,
            MemorySize = memorySize,
            NetIn = netIn,
            NetOut = netOut,
        };

    private static NodeStorageRrdData Storage(long time, long used, long size = 100 * GB)
        => new() { Time = time, Used = used, Size = size };

    [Fact]
    public void UsageStats_NoSamples_ReturnsNull()
    {
        Assert.Null(ReportEngine.GetUsageStats<VmRrdData>([], []));
    }

    [Fact]
    public void UsageStats_AverageFromAverageSeries_PeakFromMaximumSeries()
    {
        VmRrdData[] average = [Sample(0.10, 1000, 10, 100), Sample(0.30, 3000, 30, 300)];
        VmRrdData[] maximum = [Sample(0.50, 4000, 50, 500), Sample(0.90, 6000, 90, 900)];

        var stats = ReportEngine.GetUsageStats(average, maximum);

        Assert.NotNull(stats);
        Assert.Equal(2, stats.Samples);
        Assert.Equal(0.20, stats.CpuAvg, 6);
        Assert.Equal(0.90, stats.CpuPeak!.Value, 6);
        Assert.Equal(2000, stats.MemoryAvg, 6);
        Assert.Equal(6000, stats.MemoryPeak!.Value, 6);
        Assert.Equal(20, stats.NetInAvg, 6);
        Assert.Equal(90, stats.NetInPeak!.Value, 6);
        Assert.Equal(200, stats.NetOutAvg, 6);
        Assert.Equal(900, stats.NetOutPeak!.Value, 6);
    }

    [Fact]
    public void UsageStats_EmptySlots_Ignored()
    {
        // RRD slots without data come back with every value at 0, memory size included
        VmRrdData[] average = [Sample(0, 0, memorySize: 0), Sample(0.40, 4000)];
        VmRrdData[] maximum = [Sample(0, 0, memorySize: 0), Sample(0.80, 5000)];

        var stats = ReportEngine.GetUsageStats(average, maximum);

        Assert.NotNull(stats);
        Assert.Equal(1, stats.Samples);
        Assert.Equal(0.40, stats.CpuAvg, 6);
        Assert.Equal(4000, stats.MemoryAvg, 6);
    }

    [Fact]
    public void UsageStats_MaximumSeriesMissing_PeaksAreNull()
    {
        var stats = ReportEngine.GetUsageStats([Sample(0.40, 4000)], []);

        Assert.NotNull(stats);
        Assert.Equal(0.40, stats.CpuAvg, 6);
        Assert.Null(stats.CpuPeak);
        Assert.Null(stats.MemoryPeak);
        Assert.Null(stats.NetInPeak);
        Assert.Null(stats.NetOutPeak);
    }

    [Fact]
    public void StorageTrend_LessThanTwoSamples_ReturnsNull()
    {
        Assert.Null(ReportEngine.GetStorageTrend([]));
        Assert.Null(ReportEngine.GetStorageTrend([Storage(0, 10 * GB)]));
    }

    [Fact]
    public void StorageTrend_LinearGrowth_GivesGrowthPerDayAndDaysToFull()
    {
        var trend = ReportEngine.GetStorageTrend([Storage(0, 10 * GB),
                                                  Storage(Day, 12 * GB),
                                                  Storage(2 * Day, 14 * GB)]);

        Assert.NotNull(trend);
        Assert.Equal(2d * GB, trend.GrowthPerDay, 0);
        Assert.Equal(43L, trend.DaysToFull);
    }

    [Fact]
    public void StorageTrend_UnorderedSamples_SameResult()
    {
        var trend = ReportEngine.GetStorageTrend([Storage(2 * Day, 14 * GB),
                                                  Storage(0, 10 * GB),
                                                  Storage(Day, 12 * GB)]);

        Assert.NotNull(trend);
        Assert.Equal(2d * GB, trend.GrowthPerDay, 0);
        Assert.Equal(43L, trend.DaysToFull);
    }

    [Fact]
    public void StorageTrend_NotGrowing_NoDaysToFull()
    {
        var shrinking = ReportEngine.GetStorageTrend([Storage(0, 14 * GB), Storage(Day, 12 * GB)]);
        var flat = ReportEngine.GetStorageTrend([Storage(0, 14 * GB), Storage(Day, 14 * GB)]);

        Assert.NotNull(shrinking);
        Assert.Equal(-2d * GB, shrinking.GrowthPerDay, 0);
        Assert.Null(shrinking.DaysToFull);
        Assert.NotNull(flat);
        Assert.Equal(0, flat.GrowthPerDay, 0);
        Assert.Null(flat.DaysToFull);
    }

    [Fact]
    public void StorageTrend_EmptySlots_Ignored()
    {
        var trend = ReportEngine.GetStorageTrend([Storage(0, 0, size: 0),
                                                  Storage(Day, 10 * GB),
                                                  Storage(2 * Day, 12 * GB)]);

        Assert.NotNull(trend);
        Assert.Equal(2d * GB, trend.GrowthPerDay, 0);
    }

    [Fact]
    public void StorageTrend_SamplesAtSameTime_ReturnsNull()
    {
        Assert.Null(ReportEngine.GetStorageTrend([Storage(Day, 10 * GB), Storage(Day, 12 * GB)]));
    }
}
