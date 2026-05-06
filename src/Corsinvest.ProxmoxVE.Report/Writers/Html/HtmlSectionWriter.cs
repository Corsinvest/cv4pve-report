/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Writers.Html.Blocks;
using System.Text;

namespace Corsinvest.ProxmoxVE.Report.Writers.Html;

/// <summary>
/// Accumulates blocks for a single HTML page. The page is materialized to disk
/// (via <see cref="HtmlReportWriter"/>) when the writer is disposed.
/// </summary>
internal sealed class HtmlSectionWriter(HtmlReportWriter parent, string name) : ISectionWriter
{
    private readonly List<IBlock> _blocks = [];
    private bool _disposed;

    public string Name { get; } = name;

    /// <summary>Snapshot of the blocks added to this section, in insertion order.</summary>
    public IReadOnlyList<IBlock> Blocks => _blocks;

    /// <summary>Back link metadata (rendered as breadcrumb above the page title).</summary>
    public (string Label, string LinkKey)? BackLink { get; private set; }

    public void AddBackLink(string label, string linkKey)
        => BackLink = (label, linkKey);

    public void AddKeyValue(string title, IDictionary<string, object?> items)
        => _blocks.Add(new KeyValueBlock(title, items));

    public void AddKeyValueRow(params (string Title, IDictionary<string, object?> Items)[] blocks)
    {
        if (blocks.Length == 0) { return; }
        _blocks.Add(new KeyValueRowBlock(blocks));
    }

    public ITableHandle AddTable<T>(string? title, IEnumerable<T> data, TableOptions<T>? options = null)
    {
        var rows = data as List<T> ?? [.. data];
        var block = new TableBlock<T>(title, rows)
        {
            ColumnLinks = options?.ColumnLinks,
        };

        // RegisterRowKeys: register all link keys for each row up front, pointing to this page.
        // HTML doesn't have row anchors yet — we route row-level links to the section's page.
        if (options?.RegisterRowKeys != null)
        {
            foreach (var row in rows)
            {
                foreach (var key in options.RegisterRowKeys(row))
                {
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        parent.Links[key] = Name;
                    }
                }
            }
        }

        _blocks.Add(block);
        return new HtmlTableHandle<T>(title ?? "", block, rows);
    }

    public void AppendData<T>(ITableHandle table, IEnumerable<T> data)
    {
        var handle = (HtmlTableHandle<T>)table;
        handle.Rows.AddRange(data);
    }

    public void Dispose()
    {
        if (_disposed) { return; }
        _disposed = true;
        parent.NotifySectionClosed(this);
    }

    /// <summary>Renders the page body (everything inside &lt;main&gt;).
    /// <paramref name="ownPagePath"/> is prepended to in-page anchor hrefs so they
    /// resolve correctly even when a &lt;base href="../"&gt; is active on the page.</summary>
    public string RenderBody(string ownPagePath)
    {
        var sb = new StringBuilder();

        // Page-level Table of Contents — mirrors the in-sheet "Index" used in Excel.
        // Only emit it when there are at least 2 anchored blocks; on single-section
        // pages (e.g. simple lists) the TOC would be redundant.
        var anchored = _blocks
            .Where(b => b.AnchorId != null && !string.IsNullOrEmpty(b.Title))
            .ToList();

        if (anchored.Count >= 2)
        {
            sb.AppendLine("""<nav class="page-toc"><strong>Index</strong>""");
            sb.AppendLine("  <ol>");
            foreach (var b in anchored)
            {
                sb.AppendLine($"""    <li><a href="{HtmlEncoder.Attr(ownPagePath)}#{b.AnchorId}">{HtmlEncoder.Text(b.Title!)}</a></li>""");
            }
            sb.AppendLine("  </ol>");
            sb.AppendLine("</nav>");
        }

        foreach (var block in _blocks)
        {
            block.Render(sb, parent.Links);
        }
        return sb.ToString();
    }
}
