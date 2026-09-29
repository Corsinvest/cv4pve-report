/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;

namespace Corsinvest.ProxmoxVE.Report.Tests;

public class DisksSizeTests
{
    private static VmDisk Disk(string id, string? size, VmDiskKind kind = VmDiskKind.Disk, bool unused = false)
        => new() { Id = id, Size = size!, Kind = kind, IsUnused = unused };

    private static readonly VmDisk[] Mixed =
    [
        Disk("scsi0", "32G"),
        Disk("scsi1", "100G"),
        Disk("ide2", "4G", VmDiskKind.Cdrom),
        Disk("ide0", "4M", VmDiskKind.CloudInit),
        Disk("unused0", "10G", unused: true),
        Disk("unused1", "5G", unused: true),
    ];

    [Fact]
    public void DisksSize_SumsOnlyAttachedDisks()
    {
        Assert.Equal(132L * 1024 * 1024 * 1024, ReportEngine.GetDisksSize(Mixed));
    }

    [Fact]
    public void UnusedDisksSize_SumsOnlyUnusedDisks()
    {
        Assert.Equal(15L * 1024 * 1024 * 1024, ReportEngine.GetUnusedDisksSize(Mixed));
    }

    [Fact]
    public void NoSize_ReturnsNull()
    {
        // Container bind mount of a host directory
        VmDisk[] disks = [Disk("mp0", null)];

        Assert.Null(ReportEngine.GetDisksSize(disks));
        Assert.Null(ReportEngine.GetUnusedDisksSize(disks));
    }
}
