/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text;

namespace Corsinvest.ProxmoxVE.Report.Writers.Html.Blocks;

internal sealed class KeyValueBlock(string title, IDictionary<string, object?> items) : IBlock
{
    public string? Title => title;
    public string AnchorId { get; } = $"kv-{Guid.NewGuid():N}";
    string? IBlock.AnchorId => AnchorId;

    public void Render(StringBuilder sb, IDictionary<string, string> links)
    {
        var rows = string.Concat(items.Select(kv => $"""
                  <tr><th scope="row">{HtmlEncoder.Text(kv.Key)}</th><td>{HtmlEncoder.Text(FormatValue(kv.Value))}</td></tr>

            """));

        sb.Append($"""
            <section class="kv-section" id="{AnchorId}">
              <h2>{HtmlEncoder.Text(title)}</h2>
              <table class="kv">
                <tbody>
            {rows}    </tbody>
              </table>
            </section>

            """);
    }

    private static string FormatValue(object? value)
        => value switch
        {
            null => "",
            bool b => b ? "Yes" : "No",
            DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss"),
            double d => d.ToString("0.##"),
            float f => f.ToString("0.##"),
            _ => value.ToString() ?? "",
        };
}
