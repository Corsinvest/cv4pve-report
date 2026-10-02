/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Report.Helpers;
using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private const string CapacityPlanningSection = "Capacity Planning";

    internal record UsageStats(int Samples,
                               double CpuAvg,
                               double? CpuPeak,
                               double MemoryAvg,
                               double? MemoryPeak,
                               double NetInAvg,
                               double? NetInPeak,
                               double NetOutAvg,
                               double? NetOutPeak);

    internal record StorageTrend(double GrowthPerDay, long? DaysToFull);

    // The series already read by the RRD sections has one consolidation: the other one is read here,
    // so averages come from Average samples and peaks from Maximum samples.
    private static RrdDataConsolidation OtherConsolidation(RrdDataConsolidation consolidation)
        => consolidation == RrdDataConsolidation.Average
            ? RrdDataConsolidation.Maximum
            : RrdDataConsolidation.Average;

    private static (IReadOnlyList<T> Average, IReadOnlyList<T> Maximum) SplitSeries<T>(RrdDataConsolidation consolidation,
                                                                                    IReadOnlyList<T> fetched,
                                                                                    IReadOnlyList<T> other)
        => consolidation == RrdDataConsolidation.Average
            ? (fetched, other)
            : (other, fetched);

    internal static UsageStats? GetUsageStats<T>(IEnumerable<T> average, IEnumerable<T> maximum)
        where T : ICpu, IMemory, INetIO
    {
        // RRD slots without data come back with every value at 0, memory size included
        var avg = average.Where(a => a.MemorySize > 0).ToList();
        if (avg.Count == 0) { return null; }

        var max = maximum.Where(a => a.MemorySize > 0).ToList();
        double? Peak(Func<T, double> selector) => max.Count > 0 ? max.Max(selector) : null;

        return new(avg.Count,
                   avg.Average(a => a.CpuUsagePercentage),
                   Peak(a => a.CpuUsagePercentage),
                   avg.Average(a => (double)a.MemoryUsage),
                   Peak(a => a.MemoryUsage),
                   avg.Average(a => (double)a.NetIn),
                   Peak(a => a.NetIn),
                   avg.Average(a => (double)a.NetOut),
                   Peak(a => a.NetOut));
    }

    // Least squares slope of used space over time: a single prune or backup at either end
    // of the time frame does not decide the trend.
    internal static StorageTrend? GetStorageTrend(IEnumerable<NodeStorageRrdData> samples)
    {
        var valid = samples.Where(a => a.Size > 0).OrderBy(a => a.Time).ToList();
        if (valid.Count < 2) { return null; }

        var timeMean = valid.Average(a => (double)a.Time);
        var usedMean = valid.Average(a => (double)a.Used);
        var variance = valid.Sum(a => Math.Pow(a.Time - timeMean, 2));
        if (variance == 0) { return null; }

        var growthPerDay = valid.Sum(a => (a.Time - timeMean) * (a.Used - usedMean)) / variance * 86400;
        var last = valid[^1];

        return new(growthPerDay,
                   growthPerDay > 0
                    ? (long)Math.Round((last.Size - last.Used) / growthPerDay)
                    : null);
    }

    private static double? Cores(double? usage, long cpuSize)
        => usage is { } value
            ? Math.Round(value * cpuSize, 2)
            : null;

    private static double? Ratio(double? value, double total)
        => value is { } v && total > 0
            ? v / total
            : null;

    private static double? AssignedRatio(double assigned, double total)
        => total > 0
            ? Math.Round(assigned / total, 2)
            : null;

    private async Task<int> AddCapacityPlanningDataAsync()
    {
        if (!settings.CapacityPlanning.Enabled) { return 0; }

        var guests = await GetCapacityGuestsAsync();
        var nodes = await GetCapacityNodesAsync();
        var storages = GetCapacityStorages();

        _rrdGuests.Clear();
        _rrdNodes.Clear();
        _rrdStorages.Clear();
        _guestDisks.Clear();

        var count = guests.Count + nodes.Count + storages.Count;
        if (count == 0) { return 0; }

        using var sw = _writer.AddSection(CapacityPlanningSection);
        sw.AddTable("Guests",
                    guests,
                    new TableOptions<dynamic>()
                        .WithVmIdLink(r => r.VmId is long id ? id : (long?)null)
                        .WithNodeLink(r => (string?)r.Node));

        sw.AddTable("Nodes",
                    nodes,
                    new TableOptions<dynamic>().WithNodeLink(r => (string?)r.Node));

        sw.AddTable("Storage",
                    storages,
                    new TableOptions<dynamic>()
                        .WithNodeLink(r => (string?)r.Node)
                        .WithStorageLink(r => (string?)r.Storage));

        return count;
    }

    private async Task<List<dynamic>> GetCapacityGuestsAsync()
    {
        var rrd = settings.Guest.RrdData;

        var results = await RunParallelAsync(_rrdGuests, async entry =>
        {
            var item = entry.Item;
            ReportGlobal($"Capacity Guest: {item.VmId} {item.Name}");
            var other = await client.GetVmRrdDataAsync(item.Node,
                                                      item.VmType,
                                                      item.VmId,
                                                      rrd.TimeFrame,
                                                      OtherConsolidation(rrd.Consolidation))
                                    .ToSafeEnum(_issues, CapacityPlanningSection, LinkKey.Vm(item.VmId));

            var (average, maximum) = SplitSeries(rrd.Consolidation, entry.Data, other);
            return (item, stats: GetUsageStats(average, maximum));
        });

        return [.. results.Select(r =>
        {
            var (item, stats) = r;
            _guestDisks.TryGetValue(item.VmId, out var disks);

            return (dynamic)new
            {
                item.Node,
                Type = item.VmType.ToString(),
                item.VmId,
                item.Name,
                item.Status,
                item.CpuSize,
                CpuAvgPct = stats?.CpuAvg,
                CpuPeakPct = stats?.CpuPeak,
                CpuAvgCores = Cores(stats?.CpuAvg, item.CpuSize),
                CpuPeakCores = Cores(stats?.CpuPeak, item.CpuSize),
                MemorySizeGB = item.MemorySize,
                MemoryAvgGB = stats?.MemoryAvg,
                MemoryPeakGB = stats?.MemoryPeak,
                MemoryPeakUsagePct = Ratio(stats?.MemoryPeak, item.MemorySize),
                DisksSizeGB = disks.Size,
                DisksUsedGB = disks.Used,
                NetInAvgMB = stats?.NetInAvg,
                NetInPeakMB = stats?.NetInPeak,
                NetOutAvgMB = stats?.NetOutAvg,
                NetOutPeakMB = stats?.NetOutPeak,
                Samples = stats?.Samples ?? 0,
            };
        })];
    }

    private async Task<List<dynamic>> GetCapacityNodesAsync()
    {
        var rrd = settings.Node.RrdData;

        var results = await RunParallelAsync(_rrdNodes, async entry =>
        {
            var item = entry.Item;
            ReportGlobal($"Capacity Node: {item.Node}");
            var other = await client.Nodes[item.Node].Rrddata
                                    .GetAsync(rrd.TimeFrame, OtherConsolidation(rrd.Consolidation))
                                    .ToSafeEnum(_issues, CapacityPlanningSection, LinkKey.Node(item.Node));

            var (average, maximum) = SplitSeries(rrd.Consolidation, entry.Data, other);
            return (item, stats: GetUsageStats(average, maximum));
        });

        return [.. results.Select(r =>
        {
            var (item, stats) = r;

            return (dynamic)new
            {
                item.Node,
                item.Status,
                item.CpuSize,
                CpuAvgPct = stats?.CpuAvg,
                CpuPeakPct = stats?.CpuPeak,
                CpuAvgCores = Cores(stats?.CpuAvg, item.CpuSize),
                CpuPeakCores = Cores(stats?.CpuPeak, item.CpuSize),
                CpuAssigned = item.NodeCpuAssigned,
                CpuAssignedRatio = AssignedRatio(item.NodeCpuAssigned, item.CpuSize),
                MemorySizeGB = item.MemorySize,
                MemoryAvgGB = stats?.MemoryAvg,
                MemoryPeakGB = stats?.MemoryPeak,
                MemoryPeakUsagePct = Ratio(stats?.MemoryPeak, item.MemorySize),
                MemoryAssignedGB = item.NodeMemoryAssigned,
                MemoryAssignedRatio = AssignedRatio(item.NodeMemoryAssigned, item.MemorySize),
                NetInAvgMB = stats?.NetInAvg,
                NetInPeakMB = stats?.NetInPeak,
                NetOutAvgMB = stats?.NetOutAvg,
                NetOutPeakMB = stats?.NetOutPeak,
                Samples = stats?.Samples ?? 0,
            };
        })];
    }

    private List<dynamic> GetCapacityStorages()
        => [.. _rrdStorages.Select(entry =>
        {
            var item = entry.Item;
            var trend = GetStorageTrend(entry.Data);

            return (dynamic)new
            {
                Node = StorageNode(item),
                item.Storage,
                SizeGB = item.DiskSize,
                UsedGB = item.DiskUsage,
                FreeGB = item.DiskSize > item.DiskUsage
                            ? item.DiskSize - item.DiskUsage
                            : 0,
                UsagePct = item.DiskUsagePercentage,
                GrowthPerDayGB = trend?.GrowthPerDay,
                trend?.DaysToFull,
            };
        })];
}
