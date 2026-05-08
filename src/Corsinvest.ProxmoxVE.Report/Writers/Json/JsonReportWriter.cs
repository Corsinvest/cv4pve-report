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
/// schema. Sections are accumulated in memory (not streamed); typical reports
/// produce a handful of MB of JSON, well within memory budgets even on large
/// clusters.
/// </summary>
internal sealed class JsonReportWriter : IReportWriter
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly Dictionary<string, string> _links = [];
    private readonly List<JsonSectionWriter> _sections = [];
    private readonly DateTime _generatedAt = DateTime.UtcNow;
    private string? _networkDiagramSvg;
    private ReportInfo? _info;
    private Settings? _settings;
    private List<SectionStat>? _stats;

    public IDictionary<string, string> Links => _links;

    public void SetMetadata(ReportInfo info) => _info = info;

    public void SetNetworkDiagram(string svg) => _networkDiagramSvg = svg;

    public ISectionWriter AddSection(SectionId id)
    {
        _links[id.Key] = id.Key;
        var section = new JsonSectionWriter(id.Key);
        _sections.Add(section);
        return section;
    }

    public void WriteCoverPage(ReportInfo info, Settings settings, IEnumerable<SectionStat> stats)
    {
        // The "cover" is metadata in JSON: stash inputs and emit metadata.json on save.
        _settings = settings;
        _stats = stats.ToList();
    }

    public Task SaveAsync(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

        WriteEntry(zip, "metadata.json", BuildMetadata());

        foreach (var section in _sections)
        {
            var path = JsonFileName(section.Name);
            var payload = SectionPayload(section);
            WriteEntry(zip, path, Serialize(payload));
        }

        if (_networkDiagramSvg is { Length: > 0 })
        {
            WriteEntry(zip, "network-diagram.svg", _networkDiagramSvg);
        }

        return Task.CompletedTask;
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
                    result[kv.Title] = kv.Items;
                    break;
                case JsonBlock.Table tbl:
                    result[tbl.Title ?? "rows"] = tbl.Rows;
                    break;
            }
        }
        return result;
    }

    private string BuildMetadata()
    {
        var payload = new Dictionary<string, object?>
        {
            ["schemaVersion"] = 1,
            ["generatedAt"] = _generatedAt.ToString("O"),
            ["applicationName"] = _info?.ApplicationName,
            ["applicationVersion"] = _info?.ApplicationVersion,
            ["applicationUrl"] = _info?.ApplicationUrl,
        };

        if (_stats is { Count: > 0 })
        {
            payload["sections"] = _stats.Select(s => new Dictionary<string, object?>
            {
                ["name"] = s.Name,
                ["count"] = s.Count,
                ["durationSeconds"] = s.Duration.TotalSeconds,
            }).ToList();
        }

        return Serialize(payload);
    }

    private static string Serialize(object? payload)
        => JsonSerializer.Serialize(payload, JsonOpts);

    private static void WriteEntry(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false));
        writer.Write(content);
    }
}
