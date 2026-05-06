/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api;
using System.Net;

namespace Corsinvest.ProxmoxVE.Report;

internal static class PveResultExtensions
{
    /// <summary>
    /// Maps a PVE API result to a typed enumerable, returning an empty sequence when the
    /// endpoint is not implemented on the target Proxmox version (HTTP 501).
    /// Use for endpoints introduced in newer PVE releases that older clusters lack.
    /// </summary>
    public static async Task<IEnumerable<T>> ToModelEnumerableSafeAsync<T>(this Task<Result> resultTask)
    {
        var r = await resultTask;
        return r.StatusCode == HttpStatusCode.NotImplemented
            ? []
            : r.ToModel<IEnumerable<T>>();
    }
}
