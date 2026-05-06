/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Writers;

/// <summary>
/// Writes content into a single section (Excel sheet or HTML page).
/// All hyperlinks use opaque string keys (e.g. "node:cc01", "vm:100");
/// the writer resolves them to the right target when materializing.
/// </summary>
internal interface ISectionWriter : IDisposable
{
    /// <summary>"← Back" link rendered at the top of the section.</summary>
    void AddBackLink(string label, string linkKey);

    /// <summary>Two-column "key/value" block with a bold title.</summary>
    void AddKeyValue(string title, IDictionary<string, object?> items);

    /// <summary>
    /// Renders multiple key-value blocks side by side. Excel places them at increasing
    /// column offsets on the same row; HTML uses a CSS grid (responsive: stacks on narrow viewports).
    /// </summary>
    void AddKeyValueRow(params (string Title, IDictionary<string, object?> Items)[] blocks);

    /// <summary>Adds a table from the given rows. Returns a handle for later appends.
    /// When <paramref name="title"/> is null the table is rendered without a heading.</summary>
    ITableHandle AddTable<T>(string? title, IEnumerable<T> data, TableOptions<T>? options = null);

    /// <summary>Appends rows to a previously-created table.</summary>
    void AppendData<T>(ITableHandle table, IEnumerable<T> data);
}
