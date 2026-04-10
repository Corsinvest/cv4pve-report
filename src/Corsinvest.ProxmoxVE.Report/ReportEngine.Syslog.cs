/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using System.Text.RegularExpressions;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    // Mar 27 00:00:03 hostname service[pid]: message
    [GeneratedRegex(@"^(\w+\s+\d+)\s+(\d+:\d+:\d+)\s+(\S+)\s+(\S+):\s+(.*)$")]
    private static partial Regex SyslogLineRegex();

    // pvedaemon[3116203]  →  service=pvedaemon, pid=3116203
    [GeneratedRegex(@"^([^\[]+)\[(\d+)\]$")]
    private static partial Regex SyslogServicePidRegex();

    private record SyslogRow(string Node,
                             string Date,
                             string Time,
                             string Host,
                             string Service,
                             int? Pid,
                             string Message);

    private static SyslogRow ParseSyslogLine(string node, string line)
    {
        var m = SyslogLineRegex().Match(line);
        if (m.Success)
        {
            var serviceRaw = m.Groups[4].Value;
            var mp = SyslogServicePidRegex().Match(serviceRaw);
            return new SyslogRow(node,
                                 m.Groups[1].Value,
                                 m.Groups[2].Value,
                                 m.Groups[3].Value,
                                 mp.Success ? mp.Groups[1].Value : serviceRaw,
                                 mp.Success ? int.Parse(mp.Groups[2].Value) : null,
                                 m.Groups[5].Value);
        }

        return new SyslogRow(node, "", "", "", "", null, line);
    }

    private async Task<int> AddSyslogDataAsync(XLWorkbook workbook)
    {
        if (!settings.Node.Syslog.Enabled) { return 0; }

        var filtered = GetResources(ClusterResourceType.Node)
                                 .Where(a => !a.IsUnknown)
                                 .OrderBy(a => a.Id)
                                 .ToList();

        if (filtered.Count == 0) { return 0; }

        var sw = CreateSheetWriter(workbook, "Syslog");
        IXLTable? table = null;

        foreach (var item in filtered)
        {
            ReportGlobal($"Syslog: {item.Node}");

            sw.CreateOrAddTable(ref table,
                                "Syslog",
                                (await client.Nodes[item.Node]
                                       .Journal
                                       .GetAsync(lastentries: settings.Node.Syslog.Limit,
                                                 since: settings.Node.Syslog.SinceUnix,
                                                 until: settings.Node.Syslog.UntilUnix))
                                    .Select(a => ParseSyslogLine(item.Node, a)).ToList());

        }

        sw.AdjustColumns();

        return filtered.Count;
    }
}
