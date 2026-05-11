/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace Corsinvest.ProxmoxVE.Report.Writers;

internal static class ZipHelpers
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    /// <summary>Writes a UTF-8 text entry into the zip without BOM.</summary>
    public static async Task WriteTextEntryAsync(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        await using var stream = entry.Open();
        await using var writer = new StreamWriter(stream, Utf8NoBom);
        await writer.WriteAsync(content);
    }

    /// <summary>Writes the network topology SVG as <c>network-diagram.svg</c> when present.</summary>
    public static Task WriteNetworkDiagramAsync(ZipArchive zip, string? svg)
        => string.IsNullOrEmpty(svg)
            ? Task.CompletedTask
            : WriteTextEntryAsync(zip, "network-diagram.svg", svg);

    /// <summary>Copies an embedded resource from <paramref name="assembly"/> straight into a zip entry.</summary>
    public static async Task WriteEmbeddedAssetAsync(ZipArchive zip, string path, Assembly assembly, string resourceName)
    {
        await using var src = OpenEmbeddedResource(assembly, resourceName);
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        await using var dst = entry.Open();
        await src.CopyToAsync(dst);
    }

    /// <summary>Reads an embedded text resource from <paramref name="assembly"/> as a string.</summary>
    public static string ReadEmbeddedString(Assembly assembly, string resourceName)
    {
        using var src = OpenEmbeddedResource(assembly, resourceName);
        using var reader = new StreamReader(src);
        return reader.ReadToEnd();
    }

    private static Stream OpenEmbeddedResource(Assembly assembly, string resourceName)
        => assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded asset not found: {resourceName}");
}
