/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text;

namespace Corsinvest.ProxmoxVE.Report.Writers.Html.Blocks;

/// <summary>
/// A self-contained piece of HTML content (a heading, a table, a key/value list).
/// Pages are built by accumulating blocks then rendering them in order.
/// </summary>
internal interface IBlock
{
    /// <summary>Optional title shown in the per-page table-of-contents (anchor target id below).</summary>
    string? Title { get; }

    /// <summary>Stable id for in-page anchors (e.g. "table-status"). Null when no TOC entry is needed.</summary>
    string? AnchorId { get; }

    /// <summary>Append the block's HTML markup.</summary>
    void Render(StringBuilder sb, IDictionary<string, string> links);
}
