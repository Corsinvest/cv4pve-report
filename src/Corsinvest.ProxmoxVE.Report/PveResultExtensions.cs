/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Net;
using Corsinvest.ProxmoxVE.Api;

namespace Corsinvest.ProxmoxVE.Report;

internal static class PveResultExtensions
{
    public static async Task<IEnumerable<T>> ToModelEnumerableSafeAsync<T>(this Task<Result> resultTask)
    {
        var r = await resultTask;
        return r.StatusCode == HttpStatusCode.NotImplemented
            ? []
            : r.ToModel<IEnumerable<T>>();
    }
}
