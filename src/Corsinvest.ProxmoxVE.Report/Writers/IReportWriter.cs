/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Writers;

/// <summary>
/// Top-level writer for a generated report.
/// Excel materializes as a single workbook; HTML as a folder of pages.
/// </summary>
internal interface IReportWriter : IDisposable
{
    /// <summary>
    /// Cross-section link table. Sections register entries (e.g. "node:cc01" → "Node cc01")
    /// to enable hyperlinks across the report. Owned by the writer, shared with all sections.
    /// </summary>
    IDictionary<string, string> Links { get; }

    /// <summary>
    /// Sets report metadata (author, title, version, etc.).
    /// Excel maps these to workbook properties; HTML to head meta tags.
    /// </summary>
    void SetMetadata(ReportInfo info);

    /// <summary>
    /// Adds a new section to the report and returns its writer.
    /// In Excel a section becomes a sheet; in HTML a separate page.
    /// </summary>
    ISectionWriter AddSection(string name);

    /// <summary>
    /// Writes the cover/summary page for the report. Each format renders it natively
    /// (Excel: "Summary" sheet; HTML: "index.html"). Called once after all sections.
    /// </summary>
    void WriteCoverPage(ReportInfo info, Settings settings, IEnumerable<SectionStat> stats);

    /// <summary>
    /// Provides the network topology SVG to the writer. Each format decides what to do
    /// with it (Excel: no-op — the caller writes it next to the .xlsx; HTML: embedded
    /// inside the .zip as "network-diagram.svg" and linked from the sidebar/cover).
    /// </summary>
    void SetNetworkDiagram(string svg);

    /// <summary>
    /// Materializes the report to the given stream.
    /// Excel: writes the .xlsx; HTML: writes a .zip of the static folder.
    /// </summary>
    Task SaveAsync(Stream stream);
}
