/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Writers;

/// <summary>
/// Optional configuration for a table: how to register per-row anchors so
/// other sections can link to specific rows, and how to turn cell values in
/// given columns into outgoing hyperlinks.
/// </summary>
/// <typeparam name="T">Row type passed to <see cref="ISectionWriter.AddTable{T}"/>.</typeparam>
internal sealed record TableOptions<T>
{
    /// <summary>
    /// For each row, returns the link keys this row should be reachable by.
    /// Example: a row in "Nodes" for node "cc01" returns ["node:cc01"] so
    /// other tables can hyperlink their "Node" column straight to it.
    /// </summary>
    public Func<T, IEnumerable<string>>? RegisterRowKeys { get; init; }

    /// <summary>
    /// Per-column functions that turn a row value into an outgoing link key.
    /// Key = column name (e.g. "Node"); value = mapper from row to link key
    /// (e.g. row => $"node:{row.Node}"). Return null to leave the cell unlinked.
    /// </summary>
    public IDictionary<string, Func<T, string?>>? ColumnLinks { get; init; }
}
