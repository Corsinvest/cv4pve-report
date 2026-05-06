/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.IO.Compression;
using System.Text;

namespace Corsinvest.ProxmoxVE.Report.Writers.Html;

/// <summary>
/// HTML report writer. Materializes the report as a static folder
/// (index.html + per-section pages + assets/style.css + assets/sort.js)
/// packaged as a single .zip on SaveAsync.
/// </summary>
internal sealed partial class HtmlReportWriter : IReportWriter
{
    private readonly Dictionary<string, string> _links = [];
    private readonly List<HtmlSectionWriter> _sections = [];
    private readonly DateTime _generatedAt = DateTime.Now;
    private string _coverHtml = "";
    private string? _networkDiagramSvg;
    private ReportInfo? _info;

    public IDictionary<string, string> Links => _links;

    /// <summary>List of sections in insertion order, used to build the sidebar.</summary>
    internal IReadOnlyList<HtmlSectionWriter> Sections => _sections;

    public void SetMetadata(ReportInfo info) => _info = info;

    public void SetNetworkDiagram(string svg) => _networkDiagramSvg = svg;

    public ISectionWriter AddSection(string name)
    {
        // Register the logical name → file-name mapping so cross-section links can be resolved.
        _links[name] = name;

        var section = new HtmlSectionWriter(this, name);
        _sections.Add(section);
        return section;
    }

    /// <summary>Called by HtmlSectionWriter.Dispose. Currently no per-section finalization needed.</summary>
    internal void NotifySectionClosed(HtmlSectionWriter section) { /* no-op for now */ }

    public Task SaveAsync(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

        var sidebarHtml = RenderSidebar();

        // Cover page → index.html (no breadcrumb, lives at root)
        WriteEntry(zip, "index.html", RenderPage("Home", sidebarHtml, _coverHtml, breadcrumb: null, depth: 0));

        // One file per section. Detail pages are routed under sub-directories
        // (vms/, nodes/, containers/) — see HtmlEncoder.PageFileName.
        foreach (var section in _sections)
        {
            var fileName = HtmlEncoder.PageFileName(section.Name);
            var depth = HtmlEncoder.PageDepth(section.Name);
            var body = section.RenderBody(fileName);
            var breadcrumb = BuildBreadcrumb(section.BackLink);
            WriteEntry(zip, fileName, RenderPage(section.Name, sidebarHtml, body, breadcrumb, depth));
        }

        // Embedded assets (CSS + JS)
        WriteEmbeddedAsset(zip, "assets/style.css", "Corsinvest.ProxmoxVE.Report.Writers.Html.Assets.style.css");
        WriteEmbeddedAsset(zip, "assets/app.js", "Corsinvest.ProxmoxVE.Report.Writers.Html.Assets.app.js");

        // Network topology SVG (if present), bundled inside the zip alongside a
        // small HTML wrapper so it can be opened inside the main pane (with the
        // sidebar still visible) instead of replacing the whole page.
        if (_networkDiagramSvg is { Length: > 0 } svg)
        {
            // The diagram is generated with fixed width/height attributes which
            // prevents responsive scaling when embedded as <img>. Inject a viewBox
            // (using the existing width/height as the coordinate system) so the
            // browser can scale the SVG to whatever container size we give it.
            svg = MakeSvgResponsive(svg);
            WriteEntry(zip, "network-diagram.svg", svg);
            WriteEntry(zip, "network-diagram.html",
                       RenderPage("Network Diagram",
                                  sidebarHtml,
                                  body: """
                                          <a href="network-diagram.svg" target="_blank" title="Open SVG in new tab"><img src="network-diagram.svg" alt="Network topology diagram" class="diagram-frame"></a>

                                  """,
                                  breadcrumb: null,
                                  depth: 0));
        }

        return Task.CompletedTask;
    }

    public void Dispose() { }

    private static void WriteEntry(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream, new UTF8Encoding(false));
        writer.Write(content);
    }

    private static void WriteEmbeddedAsset(ZipArchive zip, string path, string resourceName)
    {
        var asm = typeof(HtmlReportWriter).Assembly;
        using var src = asm.GetManifestResourceStream(resourceName)
                          ?? throw new InvalidOperationException($"Embedded asset not found: {resourceName}");
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var dst = entry.Open();
        src.CopyTo(dst);
    }

    private string RenderPage(string title, string sidebar, string body, string? breadcrumb, int depth)
    {
        var pageTitle = HtmlEncoder.Text(title);
        var breadcrumbHtml = breadcrumb ?? "";

        // Pages in sub-directories use <base href="../"> so that all relative URLs
        // (sidebar links, asset paths, breadcrumb) resolve from the report root.
        // This keeps every page template identical regardless of nesting depth.
        var baseHref = depth == 0
            ? ""
            : $"  <base href=\"{string.Concat(Enumerable.Repeat("../", depth))}\">{Environment.NewLine}";

        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="generator" content="{{HtmlEncoder.Attr(GeneratorMeta())}}">
            {{baseHref}}  <title>{{pageTitle}} — {{HtmlEncoder.Text(_info?.ApplicationName ?? "cv4pve-report")}}</title>
              <link rel="stylesheet" href="assets/style.css">
              <script>(function(){var t=localStorage.getItem('cv4pve-theme');if(t==='light'||t==='dark')document.documentElement.setAttribute('data-theme',t);})();</script>
              <script src="assets/app.js" defer></script>
            </head>
            <body>
              <div class="layout">
            {{sidebar}}
                <main>
            {{breadcrumbHtml}}      <h1>{{pageTitle}}</h1>
            {{body}}      <p class="page-footer">{{RenderGeneratedBy()}}</p>
                </main>
              </div>
              <button type="button" id="back-to-top" class="back-to-top" aria-label="Back to top" title="Back to top">↑</button>
            </body>
            </html>
            """;
    }

    /// <summary>
    /// Inject a <c>viewBox</c> on the root <c>&lt;svg&gt;</c> element of the network diagram
    /// (and optionally drop the fixed width/height) so the browser can scale it inside an
    /// &lt;img&gt; container with <c>width: 100%; height: auto</c>.
    ///
    /// The diagram is produced by <c>NetworkDiagramBuilder</c> with explicit width/height
    /// attributes; those become the viewBox dimensions so the coordinate system is unchanged.
    /// </summary>
    private static string MakeSvgResponsive(string svg)
    {
        // Find the opening <svg ...> tag.
        var openEnd = svg.IndexOf('>');
        if (openEnd < 0) { return svg; }
        var openTag = svg[..(openEnd + 1)];
        if (openTag.Contains("viewBox=", StringComparison.OrdinalIgnoreCase)) { return svg; }

        var w = ExtractAttr(openTag, "width");
        var h = ExtractAttr(openTag, "height");
        if (string.IsNullOrEmpty(w) || string.IsNullOrEmpty(h)) { return svg; }

        // Inject viewBox just before the closing '>' of the opening tag.
        var injected = $"{openTag[..^1]} viewBox=\"0 0 {w} {h}\" preserveAspectRatio=\"xMinYMin meet\">";
        return injected + svg[(openEnd + 1)..];
    }

    private static string ExtractAttr(string tag, string attrName)
    {
        var key = $"{attrName}=\"";
        var i = tag.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (i < 0) { return ""; }
        i += key.Length;
        var j = tag.IndexOf('"', i);
        return j < 0 ? "" : tag[i..j];
    }

    private string? BuildBreadcrumb((string Label, string LinkKey)? back)
    {
        if (back is not { } b) { return null; }
        if (!_links.TryGetValue(b.LinkKey, out var target)) { return null; }
        return $"""      <p class="breadcrumb"><a href="{HtmlEncoder.PageHref(target)}">← {HtmlEncoder.Text(b.Label)}</a></p>{Environment.NewLine}""";
    }

    /// <summary>Shared "Generated by … — Corsinvest Srl … timestamp" snippet used in
    /// the cover footer and the print-only footer at the bottom of every page.</summary>
    internal string RenderGeneratedBy()
    {
        var name = HtmlEncoder.Text(_info?.ApplicationName ?? "cv4pve-report");
        var version = string.IsNullOrEmpty(_info?.ApplicationVersion) ? "" : $" v{HtmlEncoder.Text(_info.ApplicationVersion)}";
        var url = HtmlEncoder.Attr(_info?.ApplicationUrl ?? "https://github.com/Corsinvest/cv4pve-report");
        var ts = _generatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        return $"""Generated by <a href="{url}" target="_blank">{name}</a>{version} — <a href="https://www.corsinvest.it" target="_blank">Corsinvest Srl</a> — <span class="generated-at">{ts}</span>""";
    }

    /// <summary>Same as <see cref="RenderGeneratedBy"/> but without the timestamp,
    /// used in the sidebar where space is tight and the date already lives on the cover page.</summary>
    internal string RenderGeneratedByCompact()
    {
        var name = HtmlEncoder.Text(_info?.ApplicationName ?? "cv4pve-report");
        var version = string.IsNullOrEmpty(_info?.ApplicationVersion) ? "" : $" v{HtmlEncoder.Text(_info.ApplicationVersion)}";
        var url = HtmlEncoder.Attr(_info?.ApplicationUrl ?? "https://github.com/Corsinvest/cv4pve-report");
        return $"""Generated by <a href="{url}" target="_blank">{name}</a>{version} — <a href="https://www.corsinvest.it" target="_blank">Corsinvest Srl</a>""";
    }

    /// <summary>The same payload as <see cref="RenderGeneratedBy"/> but expressed as
    /// a plain "name vX.Y.Z — generated YYYY-MM-DD HH:MM:SS" string for use inside
    /// &lt;meta name="generator"&gt; tags.</summary>
    internal string GeneratorMeta()
    {
        var name = _info?.ApplicationName ?? "cv4pve-report";
        var version = string.IsNullOrEmpty(_info?.ApplicationVersion) ? "" : $" v{_info.ApplicationVersion}";
        return $"{name}{version} — generated {_generatedAt:yyyy-MM-dd HH:mm:ss}";
    }
}
