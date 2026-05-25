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

    public void Render(StringBuilder sb, Dictionary<string, string> links)
    {
        var rows = string.Concat(items.Select(kv =>
        {
            var (kind, displayLabel) = ColumnConvention.Parse(kv.Key);
            var rendered = FormatKeyValue(kv.Value, kind);
            return $"""
                          <tr><th scope="row">{HtmlEncoder.Text(displayLabel)}</th><td>{HtmlEncoder.Text(rendered)}</td></tr>

                    """;
        }));

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

    private static string FormatKeyValue(object? value, ColumnKind kind) => value switch
    {
        double dbl when kind == ColumnKind.Percentage => (dbl * 100).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "%",
        float fl when kind == ColumnKind.Percentage => (fl * 100).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "%",
        IConvertible c when kind == ColumnKind.GB => UnitFormat.BytesToGB(c.ToDouble(System.Globalization.CultureInfo.InvariantCulture)).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
        IConvertible c when kind == ColumnKind.MB => UnitFormat.BytesToMB(c.ToDouble(System.Globalization.CultureInfo.InvariantCulture)).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
        _ => BlockFormat.FormatValue(value),
    };
}
