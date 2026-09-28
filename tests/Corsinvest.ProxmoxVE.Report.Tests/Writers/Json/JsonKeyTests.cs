/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Writers.Json;

namespace Corsinvest.ProxmoxVE.Report.Tests.Writers.Json;

public class JsonKeyTests
{
    [Theory]
    [InlineData("Services", "services")]
    [InlineData("SSL Certificates", "sslCertificates")]
    [InlineData("VM ID", "vmID")]
    [InlineData("CPU Usage %", "cpuUsage")]
    [InlineData("Memory GB", "memory")]
    [InlineData("/etc/hosts", "etcHosts")]
    [InlineData("S.M.A.R.T. Data", "smartData")]
    public void FromDisplay_BuildsCamelCaseKey(string display, string expected)
    {
        Assert.Equal(expected, JsonKey.FromDisplay(display));
    }

    [Theory]
    [InlineData("IP", "ip")]
    [InlineData("IPAddress", "ipAddress")]
    [InlineData("VmId", "vmId")]
    [InlineData("CPU2", "cpu2")]
    [InlineData("Node", "node")]
    [InlineData("memoryUsage", "memoryUsage")]
    public void FromPropertyName_LowerCasesLeadingCapitals(string name, string expected)
    {
        Assert.Equal(expected, JsonKey.FromPropertyName(name));
    }
}
