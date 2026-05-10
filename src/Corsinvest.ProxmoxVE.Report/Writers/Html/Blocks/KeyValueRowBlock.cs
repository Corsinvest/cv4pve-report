/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text;

namespace Corsinvest.ProxmoxVE.Report.Writers.Html.Blocks;

internal sealed class KeyValueRowBlock(IReadOnlyList<(string Title, IDictionary<string, object?> Items)> blocks) : IBlock
{
    public string? Title => null;
    public string? AnchorId => null;

    public void Render(StringBuilder sb, Dictionary<string, string> links)
    {
        var inner = string.Concat(blocks.Select(RenderBlock));

        sb.Append($"""
            <div class="kv-row">
            {inner}</div>

            """);
    }

    private static string RenderBlock((string Title, IDictionary<string, object?> Items) block)
    {
        var rows = string.Concat(block.Items.Select(kv => $"""
                    <tr><th scope="row">{HtmlEncoder.Text(kv.Key)}</th><td>{HtmlEncoder.Text(FormatValue(kv.Value))}</td></tr>

            """));

        return $"""
              <section class="kv-section">
                <h2>{HtmlEncoder.Text(block.Title)}</h2>
                <table class="kv">
                  <tbody>
            {rows}      </tbody>
                </table>
              </section>

            """;
    }

    private static string FormatValue(object? value) => value switch
    {
        null => "",
        bool b => b ? "Yes" : "No",
        DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss"),
        double d => d.ToString("0.##"),
        float f => f.ToString("0.##"),
        _ => value.ToString() ?? "",
    };
}
