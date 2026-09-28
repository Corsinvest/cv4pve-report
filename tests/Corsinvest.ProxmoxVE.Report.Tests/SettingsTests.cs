/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.Json;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;

namespace Corsinvest.ProxmoxVE.Report.Tests;

public class SettingsTests
{
    [Fact]
    public void Serialize_WritesEnumsAsNames()
    {
        var json = JsonSerializer.Serialize(Settings.Full(), Settings.JsonOptions);
        var rrdData = JsonDocument.Parse(json).RootElement.GetProperty("Node").GetProperty("RrdData");

        Assert.Equal("Week", rrdData.GetProperty("TimeFrame").GetString());
        Assert.Equal("Average", rrdData.GetProperty("Consolidation").GetString());
    }

    [Fact]
    public void Serialize_RoundTrips()
    {
        var json = JsonSerializer.Serialize(Settings.Full(), Settings.JsonOptions);
        var settings = JsonSerializer.Deserialize<Settings>(json, Settings.JsonOptions)!;

        Assert.Equal(RrdDataTimeFrame.Week, settings.Node.RrdData.TimeFrame);
    }

    // Month rather than Day, which is the default and would pass even if the value were ignored.
    [Theory]
    [InlineData("""{ "Node": { "RrdData": { "TimeFrame": "Month" } } }""")]
    [InlineData("""{ "Node": { "RrdData": { "TimeFrame": 3 } } }""")]
    public void Deserialize_AcceptsEnumNameAndNumber(string json)
    {
        var settings = JsonSerializer.Deserialize<Settings>(json, Settings.JsonOptions)!;

        Assert.Equal(RrdDataTimeFrame.Month, settings.Node.RrdData.TimeFrame);
    }

    [Fact]
    public void SyslogLimit_IsNullWhenUntilOnly()
    {
        var syslog = new SettingsSyslog { Until = new DateOnly(2026, 1, 10) };

        Assert.Null(syslog.Limit);
    }

    [Fact]
    public void SyslogLimit_CapsAt500WhenMaxCountZero()
    {
        Assert.Equal(500, new SettingsSyslog { MaxCount = 0 }.Limit);
    }

    [Fact]
    public void FirewallRange_UsesUtcAndIncludesUntilDay()
    {
        var firewall = new SettingsFirewall
        {
            Since = new DateOnly(2026, 1, 10),
            Until = new DateOnly(2026, 1, 10)
        };

        Assert.Equal((int)new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(), firewall.SinceUnix);
        Assert.Equal(firewall.SinceUnix + 86399, firewall.UntilUnix);
    }
}
