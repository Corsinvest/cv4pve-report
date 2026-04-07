/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;

namespace Corsinvest.ProxmoxVE.Report;

public partial class ReportEngine
{
    private void AddCoverPage(XLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add("Summary");
        var row = 1;

        void Add(string value)
        {
            ws.Cell(row, 1).Value = value;
            ws.Cell(row, 1).Style.Font.SetBold(true);
            ws.Cell(row, 1).Style.Font.SetFontSize(14);
            row++;
        }

        void AddKV(string key, string value)
        {
            ws.Cell(row, 1).Value = key;
            ws.Cell(row, 2).Value = value;
            row++;
        }

        ws.Cell(row, 1).Value = "INFRASTRUCTURE REPORT";
        ws.Cell(row, 1).Style.Font.SetBold(true);
        ws.Cell(row, 1).Style.Font.SetFontSize(20);
        ws.Range(row, 1, row, 3).Merge();
        row += 2;

        Add("Report Information");

        AddKV("Generated:", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        AddKV("Application:", $"{info.ApplicationName} v{info.ApplicationVersion}");
        row++;

        Add("Filters Applied");

        AddKV("Nodes:", settings.Node.Names);
        AddKV("VMs/Containers:", settings.Guest.Ids);

        if (settings.Node.RrdData.Enabled)
        {
            AddKV("Node RRD TimeFrame:", settings.Node.RrdData.TimeFrame.ToString());
            AddKV("Node RRD Consolidation:", settings.Node.RrdData.Consolidation.ToString());
        }

        if (settings.Guest.RrdData.Enabled)
        {
            AddKV("Guest RRD TimeFrame:", settings.Guest.RrdData.TimeFrame.ToString());
            AddKV("Guest RRD Consolidation:", settings.Guest.RrdData.Consolidation.ToString());
        }

        if (settings.Storage.RrdData.Enabled)
        {
            AddKV("Storage RRD TimeFrame:", settings.Storage.RrdData.TimeFrame.ToString());
            AddKV("Storage RRD Consolidation:", settings.Storage.RrdData.Consolidation.ToString());
        }

        Add("Contents");

        var sections = new List<(string, string)>
        {
            // Topology
            ("Cluster",    "Cluster overview, users, roles, ACL, firewall, backup jobs"),
            ("Nodes",      "Node list with hardware, subscription, DNS, kernel details"),
            ("Vms",        "Virtual machines (QEMU) with agent info, OS name/version/kernel, bios, cpu, memory and disk details"),
            ("Containers", "LXC containers with hostname, swap, nameserver and privilege details"),
        };

        sections.Add(("Disks", "Global disk inventory: VM/CT disk configuration"));

        if (settings.Guest.IncludeQemuAgent)
        {
            sections.Add(("Partitions", "Guest filesystem partitions with used/total space from QEMU agent"));
        }

        if (settings.Guest.Snapshots.Enabled)
        {
            sections.Add(("Snapshots", "Global snapshot inventory across all VMs and containers"));
        }

        // Network / Storage
        sections.Add(("Network", "Global network overview: node interfaces and VM/CT network inventory"));
        sections.Add(("Storages", "Storage list with size, usage and type"));

        if (settings.Storage.Content.IncludeContent)
        {
            sections.Add(("Storage Content", "Storage content inventory (ISO, templates, disk images — excludes backups)"));
        }

        if (settings.Storage.Content.IncludeBackups)
        {
            sections.Add(("Backups", "Backup inventory across all storages with protection, encryption and verification status"));
        }

        if (settings.Firewall.Enabled)
        {
            sections.Add(("Firewall", "Global firewall rules, aliases and IPSets across cluster, nodes, VMs and containers"));
        }

        if (settings.Node.IncludeReplication)
        {
            sections.Add(("Replication", "Global replication status across all nodes: last sync, next sync, errors and duration"));
        }

        if (settings.Node.RrdData.Enabled)
        {
            sections.Add(("RRD Nodes", "Historical performance data (CPU, memory, swap, disk, network) for all nodes"));
        }

        if (settings.Storage.RrdData.Enabled)
        {
            sections.Add(("RRD Storage", "Historical performance data (size, used, usage%) for all storages"));
        }

        if (settings.Guest.RrdData.Enabled)
        {
            sections.Add(("RRD Guests", "Historical performance data (CPU, memory, disk, network) for all VMs and containers"));
        }

        if (settings.Node.Syslog.Enabled)
        {
            sections.Add(("Syslog", "Systemd journal per node parsed into date, time, host, service, pid and message"));
        }

        if (settings.Cluster.Log.Enabled)
        {
            sections.Add(("Cluster Log", "Cluster log with user, node, service and message"));
        }

        if (settings.Cluster.IncludeTasks)
        {
            sections.Add(("Cluster Tasks", "All recent tasks across the cluster with status, duration and node"));
        }

        foreach (var (sheetName, description) in sections)
        {
            ws.Cell(row, 1).Value = sheetName;
            ws.Cell(row, 1).Style.Font.SetUnderline(XLFontUnderlineValues.Single);
            ws.Cell(row, 1).Style.Font.SetFontColor(XLColor.Blue);

            var actualSheet = workbook.Worksheets.FirstOrDefault(s => s.Name.StartsWith(sheetName[..Math.Min(sheetName.Length, MaxSheetNameLength)]))?.Name ?? sheetName;
            ws.Cell(row, 1).SetHyperlink(new XLHyperlink($"'{actualSheet}'!A1"));
            ws.Cell(row, 2).Value = description;
            row++;
        }

        row += 2;
        ws.Cell(row, 1).Value = "Generated by";
        ws.Cell(row, 1).Style.Font.SetItalic(true);
        ws.Cell(row, 1).Style.Font.SetFontColor(XLColor.Gray);
        ws.Cell(row, 2).Value = info.ApplicationName;
        ws.Cell(row, 2).SetHyperlink(new XLHyperlink(info.ApplicationUrl));
        ws.Cell(row, 2).Style.Font.SetFontColor(XLColor.Blue);
        ws.Cell(row, 2).Style.Font.SetUnderline(XLFontUnderlineValues.Single);
        ws.Cell(row, 2).Style.Font.SetItalic(true);

        ws.Column(1).Width = 20;
        ws.Column(2).Width = 60;
    }
}
