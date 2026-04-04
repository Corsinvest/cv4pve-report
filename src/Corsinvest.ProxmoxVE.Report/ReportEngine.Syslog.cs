/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;

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

        return new SyslogRow(node, string.Empty, string.Empty, string.Empty, string.Empty, null, line);
    }

    private async Task AddSyslogDataAsync(XLWorkbook workbook)
    {
        if (!settings.Node.Syslog.Enabled) { return; }

        var filtered = _resources.Where(a => a.ResourceType == ClusterResourceType.Node
                                          && CheckNames(settings.Node.Names, a.Node)
                                          && !a.IsUnknown)
                                 .OrderBy(a => a.Node)
                                 .ToList();

        if (filtered.Count == 0) { return; }

        var sw = CreateSheetWriter(workbook, "Syslog");
        var ws = sw.Worksheet;

        int? limit = settings.Node.Syslog.SinceUnix.HasValue 
                        ? null 
                        : (settings.Node.Syslog.MaxEntries > 0 
                            ? settings.Node.Syslog.MaxEntries 
                            : 500);

Console.WriteLine($"Syslog: limit={limit}, since={settings.Node.Syslog.Since}, until={settings.Node.Syslog.Until}");

        IXLTable? table = null;

        foreach (var item in filtered)
        {
            ReportGlobal($"Syslog: {item.Node}");

            var response = await client.Nodes[item.Node].Journal.GetAsync(lastentries: limit,
                                                                          since: settings.Node.Syslog.SinceUnix,
                                                                          until: settings.Node.Syslog.UntilUnix
            );

            var rows = response.Select(a => ParseSyslogLine(item.Node, a)).ToList();

            if (settings.SkipEmptyTables && rows.Count == 0) { continue; }

            if (table == null)
            {
                ws.Cell(1, 1).Value = "Syslog";
                ws.Cell(1, 1).Style.Font.SetBold(true);
                table = ws.Cell(2, 1).InsertTable(rows, true);
                table.AutoFilter.IsEnabled = true;
            }
            else
            {
                table.AppendData(rows);
            }
        }

        ws.Columns().AdjustToContents();
    }
}
