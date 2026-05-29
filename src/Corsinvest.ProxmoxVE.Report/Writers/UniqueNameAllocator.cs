/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Writers;

/// <summary>
/// Allocates unique names within a single report bundle. When two sections
/// would collide on the same output target (Excel sheet name, HTML page path,
/// JSON file path) the second one gets a numeric suffix.
/// </summary>
/// <remarks>
/// Each writer instantiates its own allocator because the collision domain is
/// writer-specific: XLSX collides on sheet names (31-char, case-insensitive),
/// HTML and JSON collide on slugged file paths. The transformation up to the
/// "candidate" name is the writer's job; the allocator only handles the
/// "this candidate is already taken — give me a free variant" part.
/// </remarks>
internal sealed class UniqueNameAllocator
{
    private readonly HashSet<string> _used;
    private readonly StringComparer _comparer;

    public UniqueNameAllocator(StringComparer? comparer = null)
    {
        _comparer = comparer ?? StringComparer.Ordinal;
        _used = new HashSet<string>(_comparer);
    }

    /// <summary>
    /// Returns <paramref name="candidate"/> the first time it's asked for; on
    /// subsequent calls returns <c>candidate_2</c>, <c>candidate_3</c>, ... — skipping
    /// any suffixed variant that was already allocated as a literal candidate.
    /// </summary>
    /// <param name="candidate">The desired name (already trimmed/slugged by the caller).</param>
    /// <param name="maxLength">Hard cap on the returned name length. The suffix is
    /// guaranteed to fit by truncating the prefix. Use <see cref="int.MaxValue"/> when
    /// the target has no length constraint (HTML / JSON file names).</param>
    public string Allocate(string candidate, int maxLength = int.MaxValue)
    {
        var trimmed = candidate.Length <= maxLength ? candidate : candidate[..maxLength];

        if (_used.Add(trimmed)) { return trimmed; }

        for (var n = 2; ; n++)
        {
            var suffix = $"_{n}";
            var room = Math.Max(0, maxLength - suffix.Length);
            var prefix = trimmed.Length <= room ? trimmed : trimmed[..room];
            var candidate2 = prefix + suffix;
            if (_used.Add(candidate2)) { return candidate2; }
        }
    }
}
