/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;

namespace Corsinvest.ProxmoxVE.Report.Tests;

public class AgentDiskUsageTests
{
    private static VmQemuAgentGetFsInfo.ResultInfo Fs(string name, ulong total, ulong used, string type = "ext4")
        => new() { Name = name, TotalBytes = total, UsedBytes = used, Type = type };

    [Fact]
    public void NoFilesystems_ReturnsNull()
    {
        Assert.Null(ReportEngine.GetAgentDiskUsage([]));
    }

    [Fact]
    public void OnlyZeroSizeFilesystems_ReturnsNull()
    {
        Assert.Null(ReportEngine.GetAgentDiskUsage([Fs("sr0", 0, 0)]));
    }

    [Fact]
    public void SumsFilesystems()
    {
        var result = ReportEngine.GetAgentDiskUsage([Fs("C", 1000, 400), Fs("D", 1000, 100)]);

        Assert.NotNull(result);
        Assert.Equal(500UL, result.Value.Used);
        Assert.Equal(0.25, result.Value.Pct, 6);
    }

    [Fact]
    public void ReadOnlyImages_Ignored()
    {
        var result = ReportEngine.GetAgentDiskUsage([Fs("sda2", 1000, 400),
                                                     Fs("loop0", 50, 50, "squashfs"),
                                                     Fs("sr0", 700, 700, "iso9660")]);

        Assert.NotNull(result);
        Assert.Equal(400UL, result.Value.Used);
        Assert.Equal(0.4, result.Value.Pct, 6);
    }

    [Fact]
    public void SameDeviceMountedTwice_CountedOnce()
    {
        var result = ReportEngine.GetAgentDiskUsage([Fs("sda2", 1000, 400), Fs("sda2", 1000, 400)]);

        Assert.NotNull(result);
        Assert.Equal(400UL, result.Value.Used);
        Assert.Equal(0.4, result.Value.Pct, 6);
    }
}
