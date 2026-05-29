/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report.Tests.Writers;

public class UnitFormatTests
{
    [Fact]
    public void BytesToGB_ConvertsExactlyOneGiB()
    {
        Assert.Equal(1.0, UnitFormat.BytesToGB(1024d * 1024 * 1024), 10);
    }

    [Fact]
    public void BytesToMB_ConvertsExactlyOneMiB()
    {
        Assert.Equal(1.0, UnitFormat.BytesToMB(1024d * 1024), 10);
    }

    [Fact]
    public void BytesToGB_Zero_ReturnsZero()
    {
        Assert.Equal(0.0, UnitFormat.BytesToGB(0));
    }

    [Fact]
    public void BytesToMB_Zero_ReturnsZero()
    {
        Assert.Equal(0.0, UnitFormat.BytesToMB(0));
    }

    [Fact]
    public void BytesToGB_HalfGiB_IsHalf()
    {
        Assert.Equal(0.5, UnitFormat.BytesToGB(512d * 1024 * 1024), 10);
    }

    [Fact]
    public void BytesToMB_TenMiB_IsTen()
    {
        Assert.Equal(10.0, UnitFormat.BytesToMB(10d * 1024 * 1024), 10);
    }
}
