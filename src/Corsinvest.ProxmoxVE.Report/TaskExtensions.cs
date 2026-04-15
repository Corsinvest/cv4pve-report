/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

internal static class TaskExtensions
{
    // Awaits all tasks in parallel; individual task failures are swallowed instead of
    // surfacing as AggregateException. Faulted tasks remain IsFaulted=true, so reading
    // .Result on them would still throw — use ResultOrDefault() to read safely.
    public static Task WhenAllSafe(params Task[] tasks)
        => Task.WhenAll(tasks.Select(async t => { try { await t; } catch { } }));

    // Reads the result of a Task<T>, returning default(T) if the task did not complete
    // successfully (faulted or cancelled). Pair with WhenAllSafe to consume parallel calls
    // where individual failures must not abort the rest.
    public static T? ResultOrDefault<T>(this Task<T> task)
        => task.IsCompletedSuccessfully ? task.Result : default;
}
