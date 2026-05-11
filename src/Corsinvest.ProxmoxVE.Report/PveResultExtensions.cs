/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Net;
using Corsinvest.ProxmoxVE.Api;

namespace Corsinvest.ProxmoxVE.Report;

internal static class PveResultExtensions
{
    /// <summary>
    /// Awaits an SDK extension call returning IEnumerable&lt;T&gt;. Maps HTTP errors
    /// (via PveResultException) to Issues and returns an empty list on failure.
    /// </summary>
    public static async Task<IReadOnlyList<T>> ToSafeEnum<T>(this Task<IEnumerable<T>> task,
                                                             IssueTracker issues,
                                                             string section,
                                                             string linkKey)
    {
        try { return (await task)?.ToList() ?? []; }
        catch (PveResultException pex) { Record(issues, section, linkKey, pex); return []; }
        catch (Exception ex) when (ex is not OperationCanceledException) { issues.Warning(section, ex.Message, linkKey); return []; }
    }

    /// <summary>
    /// Single-object variant of <see cref="ToSafeEnum{T}"/>. Returns <c>default(T)</c> on failure.
    /// </summary>
    public static async Task<T?> ToSafeSingle<T>(this Task<T> task,
                                                 IssueTracker issues,
                                                 string section,
                                                 string linkKey)
    {
        try { return await task; }
        catch (PveResultException pex) { Record(issues, section, linkKey, pex); return default; }
        catch (Exception ex) when (ex is not OperationCanceledException) { issues.Warning(section, ex.Message, linkKey); return default; }
    }

    /// <summary>
    /// Awaits a raw Proxmox API call returning text payload (e.g. /etc/hosts).
    /// Records HTTP errors as Issues and returns empty string on failure.
    /// </summary>
    public static async Task<string> ToSafeText(this Task<Result> task,
                                                IssueTracker issues,
                                                string section,
                                                string linkKey)
    {
        try
        {
            var r = await task;
            if (r.IsSuccessStatusCode) { return (string?)r.ToData()?.data ?? ""; }
            var severity = ClassifyHttpStatus(r.StatusCode);
            if (severity is { } s) { issues.Add(s, section, BuildMessage(r), linkKey); }
            return "";
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { issues.Warning(section, ex.Message, linkKey); return ""; }
    }

    private static void Record(IssueTracker issues, string section, string linkKey, PveResultException pex)
    {
        var severity = ClassifyHttpStatus(pex.Result.StatusCode);
        if (severity is { } s) { issues.Add(s, section, BuildMessage(pex.Result), linkKey); }
    }

    /// <summary>
    /// Builds a diagnostic message from a Result: "&lt;code&gt; &lt;reason&gt; — &lt;api error&gt; — &lt;METHOD&gt; &lt;path&gt;".
    /// The body error and request path are included when available so the Issues page is self-contained.
    /// </summary>
    private static string BuildMessage(Result r)
    {
        var parts = new List<string> { $"{(int)r.StatusCode} {r.ReasonPhrase}" };
        var apiError = r.GetError();
        if (!string.IsNullOrWhiteSpace(apiError)) { parts.Add(apiError); }
        if (!string.IsNullOrWhiteSpace(r.RequestResource)) { parts.Add($"{r.MethodType} {r.RequestResource}"); }
        return string.Join(" — ", parts);
    }

    // 501 is silent — the endpoint doesn't exist on this PVE version, not an issue.
    // Everything else (403, 404, 5xx, network errors) is surfaced as a Warning so the
    // user can act on it.
    private static IssueSeverity? ClassifyHttpStatus(HttpStatusCode status)
        => status switch
        {
            HttpStatusCode.NotImplemented => null,
            _ => IssueSeverity.Warning,
        };
}
