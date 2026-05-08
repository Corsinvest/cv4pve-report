/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Writers.Html.Blocks;

namespace Corsinvest.ProxmoxVE.Report.Writers.Html;

internal sealed record HtmlTableHandle<T>(string Title, TableBlock<T> Block, List<T> Rows) : ITableHandle;
