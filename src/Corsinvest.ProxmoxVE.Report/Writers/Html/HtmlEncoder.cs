/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text;

namespace Corsinvest.ProxmoxVE.Report.Writers.Html;

internal static class HtmlEncoder
{
    /// <summary>Escapes text for use inside an HTML element (not in attributes).</summary>
    public static string Text(string? value)
    {
        if (string.IsNullOrEmpty(value)) { return ""; }
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"': sb.Append("&quot;"); break;
                case '\'': sb.Append("&#39;"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    /// <summary>Escapes text for use inside an HTML attribute value.</summary>
    public static string Attr(string? value) => Text(value);

    /// <summary>HTML-attribute-encoded relative href to the page named <paramref name="sectionName"/>.</summary>
    public static string PageHref(string sectionName) => Attr(PageFileName(sectionName));

    /// <summary>
    /// Translates a logical section name into a relative file path inside the report bundle.
    /// Detail pages live under category sub-directories so the root stays clean even with
    /// hundreds of VMs/containers/nodes:
    ///
    ///   "Cluster"          → "cluster.html"
    ///   "Node cc01"        → "nodes/cc01.html"
    ///   "VM 100"           → "vms/100.html"
    ///   "CT 200"           → "containers/200.html"
    /// </summary>
    public static string PageFileName(string sectionName)
    {
        // Detail-page routing: peel the prefix and put the rest under a sub-directory.
        if (sectionName.StartsWith("Node ", StringComparison.Ordinal))
        {
            return $"nodes/{Slug(sectionName["Node ".Length..])}.html";
        }
        if (sectionName.StartsWith("VM ", StringComparison.Ordinal))
        {
            return $"vms/{Slug(sectionName["VM ".Length..])}.html";
        }
        if (sectionName.StartsWith("CT ", StringComparison.Ordinal))
        {
            return $"containers/{Slug(sectionName["CT ".Length..])}.html";
        }

        // Root-level pages (the section "lists" + cluster + storage + …)
        return $"{Slug(sectionName)}.html";
    }

    /// <summary>Number of "../" needed to reach the report root from the page named
    /// <paramref name="sectionName"/>. Used to set &lt;base href&gt; on detail pages.</summary>
    public static int PageDepth(string sectionName)
        => PageFileName(sectionName).Count(c => c == '/');

    /// <summary>Slug a free-form name into a URL-safe segment (lower-case, dashes for separators).</summary>
    private static string Slug(string name)
    {
        var slug = new StringBuilder(name.Length);
        foreach (var c in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) { slug.Append(c); }
            else if (c == ' ' || c == '_' || c == '/' || c == '\\') { slug.Append('-'); }
            else if (c == '.' || c == ':') { slug.Append('-'); }
            // skip everything else
        }
        return slug.ToString();
    }
}
