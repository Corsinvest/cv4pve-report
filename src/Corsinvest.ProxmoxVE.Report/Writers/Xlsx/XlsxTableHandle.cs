/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using ClosedXML.Excel;

namespace Corsinvest.ProxmoxVE.Report.Writers.Xlsx;

internal sealed class XlsxTableHandle(string title) : ITableHandle
{
    public string Title { get; } = title;

    /// <summary>Set when the buffered <c>AddTable</c> action is replayed at <see cref="XlsxSectionWriter.Dispose"/>.</summary>
    public IXLTable? Table { get; set; }
}
