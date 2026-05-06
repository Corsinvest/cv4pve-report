/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Writers.Html.Blocks;

namespace Corsinvest.ProxmoxVE.Report.Writers.Html;

/// <summary>
/// Handle returned by <see cref="HtmlSectionWriter"/> when adding a table.
/// Carries a reference to the underlying <see cref="TableBlock{T}"/> so that
/// AppendData can extend the same table later.
/// </summary>
internal sealed record HtmlTableHandle<T>(string Title, TableBlock<T> Block, List<T> Rows) : ITableHandle;
