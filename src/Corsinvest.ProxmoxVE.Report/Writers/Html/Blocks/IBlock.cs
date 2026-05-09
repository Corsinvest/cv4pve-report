/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text;

namespace Corsinvest.ProxmoxVE.Report.Writers.Html.Blocks;

internal interface IBlock
{
    string? Title { get; }
    string? AnchorId { get; }
    void Render(StringBuilder sb, Dictionary<string, string> links);
}
