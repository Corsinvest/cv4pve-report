/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Writers.Html;

internal sealed partial class HtmlReportWriter
{
    /// <summary>
    /// Builds the sidebar shared by every page.
    ///
    /// Top-level entries that own children are rendered as &lt;details&gt; elements
    /// whose &lt;summary&gt; is itself a hyperlink to the parent page (e.g. clicking
    /// "Storage" navigates to storages.html; clicking the chevron toggles the
    /// children — Storage Content, Backups, Disks, …). Single-page sections
    /// (Network, Firewall, Replication) are rendered as flat links.
    /// </summary>
    private string RenderSidebar()
    {
        var sectionNames = _sections.Select(s => s.Name).ToHashSet();
        var sb = new System.Text.StringBuilder();

        sb.Append("""
                <aside class="sidebar">
            """);
        var appName = HtmlEncoder.Text(_info?.ApplicationName ?? "cv4pve-report");
        var appUrl = HtmlEncoder.Attr(_info?.ApplicationUrl ?? "https://github.com/Corsinvest/cv4pve-report");

        sb.AppendLine("""      <div class="sidebar-header">""");
        sb.AppendLine("""        <div class="sidebar-brand-block">""");
        sb.AppendLine($"""          <a class="sidebar-brand" href="{appUrl}" target="_blank">{appName}</a>""");
        sb.AppendLine("""          <a class="sidebar-by" href="https://www.corsinvest.it" target="_blank">by Corsinvest Srl</a>""");
        sb.AppendLine("""        </div>""");
        sb.AppendLine("""        <button type="button" id="theme-toggle" class="theme-toggle" aria-label="Toggle theme" title="Toggle light/dark theme">""");
        sb.AppendLine("""          <svg class="theme-icon-light" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41"/></svg>""");
        sb.AppendLine("""          <svg class="theme-icon-dark"  viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>""");
        sb.AppendLine("""        </button>""");
        sb.AppendLine("""      </div>""");
        sb.AppendLine("""      <input type="search" id="sidebar-filter" placeholder="Find section…" aria-label="Filter sections">""");
        sb.AppendLine("""      <nav id="sidebar-nav">""");
        sb.AppendLine("""        <a href="index.html" class="overview">Home</a>""");

        if (_networkDiagramSvg is { Length: > 0 })
        {
            sb.AppendLine("""        <a href="network-diagram.html" class="overview">Network Diagram</a>""");
        }

        // Group: Cluster (parent + children for log / tasks)
        AppendGroup(sb, sectionNames, parent: "Cluster", children: ["Cluster Log", "Cluster Tasks"]);

        // Group: Nodes (parent + per-node detail pages "Node cc01", "Node cc02", …)
        AppendGroup(sb, sectionNames, parent: "Nodes",
                    children: [.. sectionNames.Where(n => n.StartsWith("Node ")).Order()]);

        // Group: Vms (parent + per-VM detail pages)
        AppendGroup(sb, sectionNames, parent: "VMs",
                    children: [.. sectionNames.Where(n => n.StartsWith("VM ")).OrderBy(VmIdSortKey)]);

        // Group: Containers (parent + per-CT detail pages)
        AppendGroup(sb, sectionNames, parent: "Containers",
                    children: [.. sectionNames.Where(n => n.StartsWith("CT ")).OrderBy(VmIdSortKey)]);

        // Group: Storage (parent: Storages list; children: every other storage-related page)
        AppendGroup(sb, sectionNames,
                    parent: "Storages", parentLabel: "Storage",
                    children: ["Storage Content", "Backups", "Disks", "Partitions", "Snapshots"]);

        // Flat single-page sections
        AppendFlat(sb, sectionNames, "Network");
        AppendFlat(sb, sectionNames, "Firewall");
        AppendFlat(sb, sectionNames, "Replication");

        // Group: Performance (no parent page — synthetic group of RRD + Syslog pages)
        AppendSyntheticGroup(sb, sectionNames, label: "Performance",
                             children: ["RRD Nodes", "RRD Guests", "RRD Storage", "Syslog"]);

        sb.AppendLine("""      </nav>""");
        sb.Append("""    </aside>""");
        return sb.ToString();
    }

    /// <summary>
    /// Renders a top-level group. The group name itself is just a label that toggles
    /// expansion (no navigation). The first child is "Overview" — a link to the
    /// parent page (e.g. storages.html). Subsequent children are the detail pages.
    /// </summary>
    private static void AppendGroup(System.Text.StringBuilder sb,
                                    HashSet<string> known,
                                    string parent,
                                    IReadOnlyList<string> children,
                                    string? parentLabel = null)
    {
        if (!known.Contains(parent)) { return; }

        var visibleChildren = children.Where(known.Contains).ToList();
        var label = parentLabel ?? parent;

        if (visibleChildren.Count == 0)
        {
            // No children: render the parent as a flat link.
            sb.AppendLine($"""        <a href="{HtmlEncoder.PageHref(parent)}">{HtmlEncoder.Text(label)}</a>""");
            return;
        }

        // Total entries shown inside the group = Overview + children
        var totalEntries = 1 + visibleChildren.Count;

        sb.AppendLine("""        <details open>""");
        sb.AppendLine($"""          <summary>{HtmlEncoder.Text(label)} <span class="count">({totalEntries})</span></summary>""");
        sb.AppendLine($"""          <a href="{HtmlEncoder.PageHref(parent)}">Overview</a>""");
        foreach (var child in visibleChildren)
        {
            sb.AppendLine($"""          <a href="{HtmlEncoder.PageHref(child)}">{HtmlEncoder.Text(child)}</a>""");
        }
        sb.AppendLine("""        </details>""");
    }

    /// <summary>
    /// Renders a synthetic group: the summary is text only (not a link), children below.
    /// Used for "Performance" which has no overview page of its own.
    /// </summary>
    private static void AppendSyntheticGroup(System.Text.StringBuilder sb,
                                             HashSet<string> known,
                                             string label,
                                             IReadOnlyList<string> children)
    {
        var visibleChildren = children.Where(known.Contains).ToList();
        if (visibleChildren.Count == 0) { return; }

        sb.AppendLine("""        <details open>""");
        sb.AppendLine($"""          <summary>{HtmlEncoder.Text(label)} <span class="count">({visibleChildren.Count})</span></summary>""");
        foreach (var child in visibleChildren)
        {
            sb.AppendLine($"""          <a href="{HtmlEncoder.PageHref(child)}">{HtmlEncoder.Text(child)}</a>""");
        }
        sb.AppendLine("""        </details>""");
    }

    /// <summary>Single-page section rendered as a flat link (no expand/collapse).</summary>
    private static void AppendFlat(System.Text.StringBuilder sb, HashSet<string> known, string name)
    {
        if (!known.Contains(name)) { return; }
        sb.AppendLine($"""        <a href="{HtmlEncoder.PageHref(name)}">{HtmlEncoder.Text(name)}</a>""");
    }

    /// <summary>Sorting key for "VM 100" / "CT 200" entries — sorts by numeric id.</summary>
    private static int VmIdSortKey(string name)
    {
        var parts = name.Split(' ');
        return parts.Length >= 2 && int.TryParse(parts[1], out var id) ? id : int.MaxValue;
    }
}
