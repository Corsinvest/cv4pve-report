/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report.Tests.Writers;

public class UniqueNameAllocatorTests
{
    [Fact]
    public void Allocate_FirstCall_ReturnsCandidateUnchanged()
    {
        var allocator = new UniqueNameAllocator();

        Assert.Equal("nodes/cc01.html", allocator.Allocate("nodes/cc01.html"));
    }

    [Fact]
    public void Allocate_SecondCallSameCandidate_GetsSuffix2()
    {
        var allocator = new UniqueNameAllocator();

        Assert.Equal("nodes/cc01.html", allocator.Allocate("nodes/cc01.html"));
        Assert.Equal("nodes/cc01.html_2", allocator.Allocate("nodes/cc01.html"));
    }

    [Fact]
    public void Allocate_ThirdCall_GetsSuffix3()
    {
        var allocator = new UniqueNameAllocator();

        allocator.Allocate("Cluster");
        allocator.Allocate("Cluster");
        Assert.Equal("Cluster_3", allocator.Allocate("Cluster"));
    }

    [Fact]
    public void Allocate_DistinctCandidates_AreIndependent()
    {
        var allocator = new UniqueNameAllocator();

        Assert.Equal("a", allocator.Allocate("a"));
        Assert.Equal("b", allocator.Allocate("b"));
        Assert.Equal("a_2", allocator.Allocate("a"));
        Assert.Equal("b_2", allocator.Allocate("b"));
    }

    [Fact]
    public void Allocate_LiteralSuffixedNameAfterBase_SkipsCollision()
    {
        // Edge case: "cc01" is taken, then someone asks for the literal "cc01_2".
        // The allocator must hand out "cc01_2" exactly once and then start adding
        // a NEW suffix when collision happens — never reuse "cc01_2" twice.
        var allocator = new UniqueNameAllocator();

        Assert.Equal("cc01", allocator.Allocate("cc01"));
        Assert.Equal("cc01_2", allocator.Allocate("cc01_2"));
        Assert.Equal("cc01_3", allocator.Allocate("cc01"));    // skips the already-taken cc01_2
    }

    [Fact]
    public void Allocate_RespectsCaseInsensitiveComparer()
    {
        // XLSX uses OrdinalIgnoreCase because Excel sheet names are case-insensitive.
        var allocator = new UniqueNameAllocator(StringComparer.OrdinalIgnoreCase);

        Assert.Equal("Node", allocator.Allocate("Node"));
        Assert.Equal("node_2", allocator.Allocate("node"));
    }

    [Fact]
    public void Allocate_TruncatesToMaxLength_OnFirstCall()
    {
        var allocator = new UniqueNameAllocator();

        // Excel sheet-name limit is 31.
        var result = allocator.Allocate("ThisNameIsLongerThanThirtyOneChars", maxLength: 31);

        Assert.Equal(31, result.Length);
        Assert.Equal("ThisNameIsLongerThanThirtyOneCh", result);
    }

    [Fact]
    public void Allocate_TruncatesPrefixToFitSuffix_OnCollision()
    {
        var allocator = new UniqueNameAllocator();

        // 31-char limit with "_2" suffix → prefix must be 29 chars.
        allocator.Allocate("ThisNameIsLongerThanThirtyOneCh", maxLength: 31);
        var second = allocator.Allocate("ThisNameIsLongerThanThirtyOneChars", maxLength: 31);

        Assert.Equal(31, second.Length);
        Assert.EndsWith("_2", second);
    }

    [Fact]
    public void Allocate_KnownCollision_NodeWithDotVsDash()
    {
        // The real-world bug this helper guards against: two PVE nodes named
        // "cc.01" and "cc-01" both slug to the same path. The first one wins;
        // the second one gets a numeric suffix.
        var allocator = new UniqueNameAllocator();
        const string slugged = "nodes/cc-01.html";

        Assert.Equal("nodes/cc-01.html", allocator.Allocate(slugged));
        Assert.Equal("nodes/cc-01.html_2", allocator.Allocate(slugged));
    }
}
