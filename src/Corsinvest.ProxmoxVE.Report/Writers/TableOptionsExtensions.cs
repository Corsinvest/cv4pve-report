/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Writers;

/// <summary>
/// Shortcut builders for the most common <see cref="TableOptions{T}"/> patterns
/// (Node / VmId / Storage / Replication links). Mirror the old
/// <c>SheetWriter.ApplyXxxLinks</c> helpers in a strongly-typed, format-agnostic way.
/// </summary>
internal static class TableOptionsExtensions
{
    /// <summary>Adds a "Node" column link mapping rows via the given selector.</summary>
    public static TableOptions<T> WithNodeLink<T>(this TableOptions<T> options, Func<T, string?> nodeSelector)
        => options.WithColumnLink("Node", row => nodeSelector(row) is { Length: > 0 } n ? $"node:{n}" : null);

    /// <summary>Adds a "VmId" column link mapping rows via the given selector.</summary>
    public static TableOptions<T> WithVmIdLink<T>(this TableOptions<T> options, Func<T, long?> vmIdSelector)
        => options.WithColumnLink("VmId", row => vmIdSelector(row) is long id and > 0 ? $"vm:{id}" : null);

    /// <summary>Adds a "Storage" column link (single shared key — the storage page anchor).</summary>
    public static TableOptions<T> WithStorageLink<T>(this TableOptions<T> options, Func<T, string?> storageSelector)
        => options.WithColumnLink("Storage", row => string.IsNullOrWhiteSpace(storageSelector(row)) ? null : "storage:link");

    /// <summary>Adds replication-style links: Node, VmId, Source (node), Target (node).</summary>
    public static TableOptions<T> WithReplicationLinks<T>(this TableOptions<T> options,
                                                          Func<T, string?> nodeSelector,
                                                          Func<T, long?> vmIdSelector,
                                                          Func<T, string?> sourceSelector,
                                                          Func<T, string?> targetSelector)
        => options.WithNodeLink(nodeSelector)
                  .WithVmIdLink(vmIdSelector)
                  .WithColumnLink("Source", row => sourceSelector(row) is { Length: > 0 } s ? $"node:{s}" : null)
                  .WithColumnLink("Target", row => targetSelector(row) is { Length: > 0 } t ? $"node:{t}" : null);

    /// <summary>Generic per-column link builder; preserves any previously configured links.</summary>
    public static TableOptions<T> WithColumnLink<T>(this TableOptions<T> options, string columnName, Func<T, string?> mapper)
    {
        var dict = options.ColumnLinks != null
            ? new Dictionary<string, Func<T, string?>>(options.ColumnLinks)
            : [];
        dict[columnName] = mapper;
        return options with { ColumnLinks = dict };
    }

    /// <summary>Sets the per-row anchor mapper.</summary>
    public static TableOptions<T> WithRowKeys<T>(this TableOptions<T> options, Func<T, IEnumerable<string>> rowKeys)
        => options with { RegisterRowKeys = rowKeys };
}
