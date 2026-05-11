/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Globalization;

namespace Corsinvest.ProxmoxVE.Report.Writers.Html.Blocks;

internal static class BlockFormat
{
    /// <summary>Renders a key/value cell value: bool → Yes/No, dates → ISO-8601, numbers → "0.##" invariant.</summary>
    public static string FormatValue(object? value)
        => value switch
        {
            null => "",
            bool b => b ? "Yes" : "No",
            DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            double d => d.ToString("0.##", CultureInfo.InvariantCulture),
            float f => f.ToString("0.##", CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "",
        };
}
