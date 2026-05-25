/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.IO.Compression;
using System.Text.Json;

namespace Corsinvest.ProxmoxVE.Report.Writers.Html;

internal sealed partial class HtmlReportWriter(ReportInfo info) : IReportWriter
{
    private readonly List<HtmlSectionWriter> _sections = [];
    private readonly DateTime _generatedAt = DateTime.Now;
    private readonly ReportInfo _info = info;
    private string _coverHtml = "";
    private string? _networkDiagramSvg;

    public Dictionary<string, string> Links { get; } = [];

    public void SetNetworkDiagram(string svg) => _networkDiagramSvg = svg;

    public ISectionWriter AddSection(SectionId id)
    {
        Links[id.Key] = id.Key;
        if (LinkKey.ForSection(id.Key) is { } sectionKey) { Links[sectionKey] = id.Key; }
        if (id is SectionId.Compliance cp) { Links[LinkKey.CompliancePack(cp.PackId)] = id.Key; }

        var displayName = id switch
        {
            SectionId.Vm v => string.IsNullOrWhiteSpace(v.DisplayLabel)
                                ? v.Id.ToString()
                                : $"{v.Id} — {v.DisplayLabel}",

            SectionId.Container c => string.IsNullOrWhiteSpace(c.DisplayLabel)
                                        ? c.Id.ToString()
                                        : $"{c.Id} — {c.DisplayLabel}",

            SectionId.Node n => n.Hostname,
            SectionId.Compliance cp2 => $"Compliance — {cp2.PackTitle}",
            _ => id.Key,
        };

        var section = new HtmlSectionWriter(this, id.Key, displayName);
        _sections.Add(section);
        return section;
    }

    public async Task SaveAsync(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

        var sidebarHtml = RenderSidebar();

        await ZipHelpers.WriteTextEntryAsync(zip, "index.html", RenderPage("Summary", sidebarHtml, _coverHtml, depth: 0));

        foreach (var section in _sections)
        {
            var fileName = HtmlEncoder.PageFileName(section.Name);
            var depth = HtmlEncoder.PageDepth(section.Name);
            var body = section.RenderBody(fileName);
            await ZipHelpers.WriteTextEntryAsync(zip, fileName, RenderPage(section.DisplayName, sidebarHtml, body, depth));
        }

        var asm = typeof(HtmlReportWriter).Assembly;
        await ZipHelpers.WriteEmbeddedAssetAsync(zip, "assets/style.css", asm, "Corsinvest.ProxmoxVE.Report.Writers.Html.Assets.style.css");
        await ZipHelpers.WriteEmbeddedAssetAsync(zip, "assets/app.js", asm, "Corsinvest.ProxmoxVE.Report.Writers.Html.Assets.app.js");
        await ZipHelpers.WriteEmbeddedAssetAsync(zip, "assets/table.js", asm, "Corsinvest.ProxmoxVE.Report.Writers.Html.Assets.table.js");
        await ZipHelpers.WriteTextEntryAsync(zip, "assets/sidebar-data.js", RenderSidebarData());
        await ZipHelpers.WriteTextEntryAsync(zip, "assets/export-data.js", RenderExportData());

        // HTML wraps the SVG in a small page so it opens inside <main> with the sidebar visible.
        if (_networkDiagramSvg is { Length: > 0 } svg)
        {
            await ZipHelpers.WriteTextEntryAsync(zip, "network-diagram.svg", svg);
            await ZipHelpers.WriteTextEntryAsync(zip,
                                                 "network-diagram.html",
                                                 RenderPage("Network Diagram",
                                                            sidebarHtml,
                                                            body: """
                                                                    <a href="network-diagram.svg" target="_blank" title="Open SVG in new tab"><img src="network-diagram.svg" alt="Network topology diagram" class="diagram-frame"></a>

                                                            """,
                                                            depth: 0,
                                                            showExport: false));
        }
    }

    public void Dispose() { }

    /// <summary>
    /// Builds the lazy-loaded export data file: declares <c>window.__REPORT_CSS__</c>
    /// and <c>window.__REPORT_TABLE_JS__</c> as inlined string literals so the
    /// <c>exportPage()</c> handler in <c>app.js</c> can build a self-contained
    /// HTML download even when the page is opened from <c>file://</c>.
    /// </summary>
    private static string RenderExportData()
    {
        var asm = typeof(HtmlReportWriter).Assembly;
        var css = ZipHelpers.ReadEmbeddedString(asm, "Corsinvest.ProxmoxVE.Report.Writers.Html.Assets.style.css");
        var tableJs = ZipHelpers.ReadEmbeddedString(asm, "Corsinvest.ProxmoxVE.Report.Writers.Html.Assets.table.js");

        return $"""
            window.__REPORT_CSS__ = {JsonSerializer.Serialize(css)};
            window.__REPORT_TABLE_JS__ = {JsonSerializer.Serialize(tableJs)};
            """;
    }

    private string RenderPage(string title, string sidebar, string body, int depth, bool showExport = true)
    {
        var pageTitle = HtmlEncoder.Text(title);

        // Pages in sub-directories use <base href="../"> so that all relative URLs
        // (sidebar links, asset paths) resolve from the report root.
        // This keeps every page template identical regardless of nesting depth.
        var baseHref = depth == 0
                        ? ""
                        : $"  <base href=\"{string.Concat(Enumerable.Repeat("../", depth))}\">{Environment.NewLine}";

        var pageActions = !showExport ? "" : """
                  <div class="page-actions">
                    <button type="button" id="export-btn" class="page-action" aria-label="Export standalone HTML" title="Export standalone HTML">
                      <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
                    </button>
                  </div>
            """;

        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="generator" content="{{HtmlEncoder.Attr(GeneratorMeta())}}">
            {{baseHref}}  <title>{{pageTitle}} — {{HtmlEncoder.Text(_info.ApplicationName)}}</title>
              <link rel="icon" type="image/svg+xml" href="data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 200 200'><rect width='200' height='200' rx='6' ry='6' fill='%23163454'/><path fill='%23ffffff' d='M 78 22 Q 64 22 60 38 L 24 162 Q 20 178 36 178 L 110 178 Q 124 178 128 162 L 138 124 L 110 124 L 102 156 L 60 156 L 90 44 L 132 44 L 124 76 L 152 76 L 162 38 Q 166 22 152 22 Z'/></svg>">
              <link rel="stylesheet" href="assets/style.css">
              <script>(function(){var t=localStorage.getItem('cv4pve-theme');if(t==='light'||t==='dark')document.documentElement.setAttribute('data-theme',t);})();</script>
              <script src="assets/sidebar-data.js" defer></script>
              <script src="assets/app.js" defer></script>
              <script src="assets/table.js" defer></script>
            </head>
            <body>
              <div class="layout">
            {{sidebar}}
                <main>
                  <div class="page-header"><h1>{{pageTitle}}</h1>{{pageActions}}</div>
            {{body}}      <p class="page-footer">{{RenderGeneratedBy()}}</p>
                </main>
              </div>
              <button type="button" id="back-to-top" class="back-to-top" aria-label="Back to top" title="Back to top">↑</button>
            </body>
            </html>
            """;
    }

    /// <summary>Shared "Generated by … — Corsinvest Srl … timestamp" snippet used in
    /// the cover footer and the print-only footer at the bottom of every page.</summary>
    internal string RenderGeneratedBy()
    {
        var name = HtmlEncoder.Text(_info.ApplicationName);
        var version = string.IsNullOrEmpty(_info.ApplicationVersion)
                         ? ""
                         : $" v{HtmlEncoder.Text(_info.ApplicationVersion)}";

        var url = HtmlEncoder.Attr(_info.ApplicationUrl);
        var ts = _generatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        return $"""Generated by <a href="{url}" target="_blank">{name}</a>{version} — <a href="https://www.corsinvest.it" target="_blank">Corsinvest Srl</a> — <span class="generated-at">{ts}</span>""";
    }

    /// <summary>The same payload as <see cref="RenderGeneratedBy"/> but expressed as
    /// a plain "name vX.Y.Z — generated YYYY-MM-DD HH:MM:SS" string for use inside
    /// &lt;meta name="generator"&gt; tags.</summary>
    private string GeneratorMeta()
    {
        var version = string.IsNullOrEmpty(_info.ApplicationVersion)
                         ? ""
                         : $" v{_info.ApplicationVersion}";
        return $"{_info.ApplicationName}{version} — generated {_generatedAt:yyyy-MM-dd HH:mm:ss}";
    }
}
