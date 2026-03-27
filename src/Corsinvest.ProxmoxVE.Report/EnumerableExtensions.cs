/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

internal static class EnumerableExtensions
{
    public static string JoinAsString<T>(this IEnumerable<T> source, string separator)
        => string.Join(separator, source);
}
