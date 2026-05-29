/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text;
using Corsinvest.ProxmoxVE.Report.Writers.Html.Blocks;

namespace Corsinvest.ProxmoxVE.Report.Writers.Html;

internal sealed class HtmlSectionWriter(HtmlReportWriter parent, string name, string displayName, string fileName) : ISectionWriter
{
    private readonly List<IBlock> _blocks = [];
    public string Name { get; } = name;
    public string DisplayName { get; } = displayName;
    /// <summary>Final, collision-free file path inside the zip (e.g. <c>nodes/cc01.html</c>).</summary>
    public string FileName { get; } = fileName;
    public IReadOnlyList<IBlock> Blocks => _blocks;

    public void AddBackLink(string label, string linkKey) { }

    // First KV block in the section is the page header — title becomes the fixed "Info"
    // heading (mirrors the JSON writer's i == 0 → "info" rule applied across all sections).
    public void AddKeyValue(string title, IDictionary<string, object?> items)
        => _blocks.Add(new KeyValueBlock(IsFirstKv() ? "Info" : title, items));

    public void AddKeyValueRow(params (string Title, IDictionary<string, object?> Items)[] blocks)
    {
        if (blocks.Length == 0) { return; }
        _blocks.Add(new KeyValueRowBlock(blocks));
    }

    private bool IsFirstKv() => !_blocks.Any(b => b is KeyValueBlock or KeyValueRowBlock);

    public ITableHandle AddTable<T>(string? title, IEnumerable<T> data, TableOptions<T>? options = null)
    {
        var rows = data as List<T> ?? [.. data];
        var block = new TableBlock<T>(title, rows)
        {
            ColumnLinks = options?.ColumnLinks,
        };

        // HTML has no row anchors yet — row-level links resolve to the section's page.
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

    public void Dispose() { }

    /// <summary>Renders the page body (everything inside &lt;main&gt;).
    /// <paramref name="ownPagePath"/> is prepended to in-page anchor hrefs so they
    /// resolve correctly even when a &lt;base href="../"&gt; is active on the page.</summary>
    public string RenderBody(string ownPagePath)
    {
        var sb = new StringBuilder();

        // Page-level TOC (mirrors the in-sheet "Index" used in Excel) — only useful
        // when the page has 2+ anchored blocks, otherwise redundant with the <h1>.
        var anchored = _blocks.Where(b => b.AnchorId != null && !string.IsNullOrEmpty(b.Title))
                              .ToList();

        if (anchored.Count >= 2)
        {
            sb.AppendLine("""<nav class="page-toc"><strong>Index</strong>""");
            sb.AppendLine("  <ol>");
            foreach (var item in anchored)
            {
                sb.AppendLine($"""    <li><a href="{HtmlEncoder.Attr(ownPagePath)}#{item.AnchorId}">{HtmlEncoder.Text(item.Title)}</a></li>""");
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
