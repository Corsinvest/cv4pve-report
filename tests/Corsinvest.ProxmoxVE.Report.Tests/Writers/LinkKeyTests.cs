/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Writers;

namespace Corsinvest.ProxmoxVE.Report.Tests.Writers;

public class LinkKeyTests
{
    [Fact]
    public void Node_BuildsNodeKey()
    {
        Assert.Equal("node:cc01", LinkKey.Node("cc01"));
    }

    [Fact]
    public void Vm_BuildsVmKeyFromId()
    {
        Assert.Equal("vm:100", LinkKey.Vm(100));
    }

    [Fact]
    public void Storage_BuildsCompositeKey()
    {
        Assert.Equal("storage:cc01:local-zfs", LinkKey.Storage("cc01", "local-zfs"));
    }

    [Fact]
    public void NodeNetwork_BuildsCompositeKey()
    {
        Assert.Equal("node:cc01:network:vmbr0", LinkKey.NodeNetwork("cc01", "vmbr0"));
    }

    [Theory]
    [InlineData("Cluster", "section:cluster")]
    [InlineData("Cluster Access", "section:cluster-access")]
    [InlineData("Storages", "section:storages")]
    [InlineData("RRD Nodes", "section:rrd-nodes")]
    [InlineData("Firewall", "section:firewall")]
    public void ForSection_KnownSection_ReturnsCanonicalKey(string sectionName, string expected)
    {
        Assert.Equal(expected, LinkKey.ForSection(sectionName));
    }

    [Theory]
    [InlineData("Node cc01")]
    [InlineData("VM 100")]
    [InlineData("CT 200")]
    [InlineData("Unknown Section")]
    public void ForSection_DetailOrUnknown_ReturnsNull(string sectionName)
    {
        Assert.Null(LinkKey.ForSection(sectionName));
    }

    [Fact]
    public void ForSection_KnownSections_AreUnique()
    {
        var allSections = new[]
        {
            "Cluster", "Cluster Access", "Cluster SDN", "Cluster HA", "Cluster Pools",
            "Cluster Log", "Cluster Tasks", "Storages", "Storage Content", "Backups",
            "Disks", "Partitions", "Snapshots", "Network", "Firewall", "Replication",
            "RRD Nodes", "RRD Storage", "RRD Guests", "Syslog", "Issues",
        };

        var keys = allSections.Select(LinkKey.ForSection).ToList();

        Assert.All(keys, k => Assert.NotNull(k));
        Assert.Equal(allSections.Length, keys.Distinct().Count());
    }
}
