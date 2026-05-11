/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Corsinvest.ProxmoxVE.Report.Writers.Json;

/// <summary>
/// JSON report writer. Captures every section the engine emits and materialises
/// the report as a multi-file zip with one file per section plus per-resource
/// detail files for nodes / VMs / containers — see docs/json-format.md for the
/// schema. Each section is serialised straight into its zip entry (no intermediate
/// string), so the peak memory footprint stays bounded even on very large clusters.
/// </summary>
internal sealed partial class JsonReportWriter(ReportInfo info) : IReportWriter
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly List<JsonSectionWriter> _sections = [];
    private readonly DateTime _generatedAt = DateTime.UtcNow;
    private readonly ReportInfo _info = info;
    private List<SectionStat> _stats = [];
    private Settings? _settings;
    private string? _networkDiagramSvg;

    public Dictionary<string, string> Links { get; } = [];

    public void SetNetworkDiagram(string svg) => _networkDiagramSvg = svg;

    public ISectionWriter AddSection(SectionId id)
    {
        Links[id.Key] = id.Key;

        // Auto-register the canonical section:* key so cross-section references resolve
        // (mirrors the Xlsx/Html writers).
        if (LinkKey.ForSection(id.Key) is { } sectionKey) { Links[sectionKey] = id.Key; }

        var section = new JsonSectionWriter(id.Key);
        _sections.Add(section);
        return section;
    }

    public void WriteCoverPage(Settings settings, IEnumerable<SectionStat> stats)
    {
        _settings = settings;
        _stats = [.. stats];
    }

    public async Task SaveAsync(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

        await WriteJsonEntryAsync(zip, "metadata.json", BuildMetadata());

        foreach (var section in _sections)
        {
            var path = JsonFileName(section.Name);
            var payload = SectionPayload(section);
            await WriteJsonEntryAsync(zip, path, payload);
        }

        if (_networkDiagramSvg is { Length: > 0 })
        {
            WriteTextEntry(zip, "network-diagram.svg", _networkDiagramSvg);
        }
    }

    public void Dispose() { }

    /// <summary>
    /// Translate a logical section name to its file path inside the zip:
    ///   "Cluster"     -> "cluster.json"
    ///   "Node cc01"   -> "nodes/cc01.json"
    ///   "VM 100"      -> "vms/100.json"
    ///   "CT 200"      -> "containers/200.json"
    ///   "RRD Nodes"   -> "rrd-nodes.json"
    /// </summary>
    private static string JsonFileName(string sectionName)
    {
        if (sectionName.StartsWith("Node ", StringComparison.Ordinal))
        {
            return $"nodes/{Slug(sectionName["Node ".Length..])}.json";
        }
        else if (sectionName.StartsWith("VM ", StringComparison.Ordinal))
        {
            return $"vms/{Slug(sectionName["VM ".Length..])}.json";
        }
        else if (sectionName.StartsWith("CT ", StringComparison.Ordinal))
        {
            return $"containers/{Slug(sectionName["CT ".Length..])}.json";
        }
        else
        {
            return $"{Slug(sectionName)}.json";
        }
    }

    private static string Slug(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) { sb.Append(c); }
            else if (c == ' ' || c == '_' || c == '/' || c == '\\') { sb.Append('-'); }
            else if (c == '.' || c == ':') { sb.Append('-'); }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Build the payload for a section. A section that contains a single Table block
    /// (e.g. "Nodes" overview) collapses to the array directly. A section with multiple
    /// blocks (e.g. "Cluster" with users / tokens / groups / …, or a per-VM detail with
    /// keyValue + table mixes) is emitted as an object keyed by block title.
    /// </summary>
    private static object? SectionPayload(JsonSectionWriter section)
    {
        if (section.Blocks.Count == 0) { return new Dictionary<string, object?>(); }

        // Single table → return its rows directly (overview lists like vms.json / nodes.json).
        if (section.Blocks.Count == 1 && section.Blocks[0] is JsonBlock.Table singleTable)
        {
            return singleTable.Rows;
        }

        var result = new Dictionary<string, object?>();
        foreach (var block in section.Blocks)
        {
            switch (block)
            {
                case JsonBlock.KeyValue kv:
                    result[JsonKey.FromDisplay(kv.Title)] = kv.Items;
                    break;

                case JsonBlock.Table tbl:
                    result[tbl.Title is null ? "rows" : JsonKey.FromDisplay(tbl.Title)] = tbl.Rows;
                    break;
            }
        }
        return result;
    }

    /// <summary>
    /// Serialise <paramref name="payload"/> straight into a new zip entry,
    /// without buffering the JSON as an intermediate string. This keeps the
    /// peak memory bounded even when a single section serialises to tens of MB
    /// (e.g. RRD data on large clusters).
    /// </summary>
    private static async Task WriteJsonEntryAsync(ZipArchive zip, string path, object? payload)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        await JsonSerializer.SerializeAsync(stream, payload, JsonOpts);
    }

    private static void WriteTextEntry(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }
}
