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
        // First block → fixed "Info" heading (mirrors the JSON writer's i == 0 → "info" rule).
        var inner = string.Concat(blocks.Select((b, i) => RenderBlock(i == 0 ? "Info" : b.Title, b.Items)));

        sb.Append($"""
            <div class="kv-row">
            {inner}</div>

            """);
    }

    private static string RenderBlock(string title, IDictionary<string, object?> items)
    {
        var rows = string.Concat(items.Select(kv =>
        {
            var (kind, displayLabel) = ColumnConvention.Parse(kv.Key);
            var rendered = FormatKeyValue(kv.Value, kind);
            return $"""
                        <tr><th scope="row">{HtmlEncoder.Text(displayLabel)}</th><td>{HtmlEncoder.Text(rendered)}</td></tr>

                """;
        }));

        return $"""
              <section class="kv-section">
                <h2>{HtmlEncoder.Text(title)}</h2>
                <table class="kv">
                  <tbody>
            {rows}      </tbody>
                </table>
              </section>

            """;
    }

    private static string FormatKeyValue(object? value, ColumnKind kind) => value switch
    {
        double dbl when kind == ColumnKind.Percentage => (dbl * 100).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "%",
        float fl when kind == ColumnKind.Percentage => (fl * 100).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "%",
        IConvertible c when kind == ColumnKind.GB => UnitFormat.BytesToGB(c.ToDouble(System.Globalization.CultureInfo.InvariantCulture)).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
        IConvertible c when kind == ColumnKind.MB => UnitFormat.BytesToMB(c.ToDouble(System.Globalization.CultureInfo.InvariantCulture)).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
        _ => BlockFormat.FormatValue(value),
    };
}
