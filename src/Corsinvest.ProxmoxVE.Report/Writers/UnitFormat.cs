/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Writers;

/// <summary>
/// Shared byte-to-unit conversion helpers used by the XLSX and HTML writers
/// when rendering columns with the <c>*GB</c> / <c>*MB</c> naming convention.
/// The engine passes raw byte counts (the SDK's native unit); each writer
/// converts at render time so JSON can expose the raw byte values directly.
/// </summary>
internal static class UnitFormat
{
    private const double BytesPerMB = 1024d * 1024d;
    private const double BytesPerGB = 1024d * 1024d * 1024d;

    public static double BytesToGB(double bytes) => bytes / BytesPerGB;
    public static double BytesToMB(double bytes) => bytes / BytesPerMB;
}
