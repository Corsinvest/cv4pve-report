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
    public static TableOptions<T> WithNodeLink<T>(this TableOptions<T> options, Func<T, string?> nodeSelector)
        => options.WithColumnLink("Node", row => nodeSelector(row) is { Length: > 0 } n ? LinkKey.Node(n) : null);

    public static TableOptions<T> WithVmIdLink<T>(this TableOptions<T> options, Func<T, long?> vmIdSelector)
        => options.WithColumnLink("VmId", row => vmIdSelector(row) is long id and > 0 ? LinkKey.Vm(id) : null);

    public static TableOptions<T> WithStorageLink<T>(this TableOptions<T> options, Func<T, string?> storageSelector)
        => options.WithColumnLink("Storage", row => string.IsNullOrWhiteSpace(storageSelector(row)) ? null : LinkKey.Storages);

    /// <summary>Replication tables link Node, VmId, Source (node) and Target (node) in one go.</summary>
    public static TableOptions<T> WithReplicationLinks<T>(this TableOptions<T> options,
                                                          Func<T, string?> nodeSelector,
                                                          Func<T, long?> vmIdSelector,
                                                          Func<T, string?> sourceSelector,
                                                          Func<T, string?> targetSelector)
        => options.WithNodeLink(nodeSelector)
                  .WithVmIdLink(vmIdSelector)
                  .WithColumnLink("Source", row => sourceSelector(row) is { Length: > 0 } s ? LinkKey.Node(s) : null)
                  .WithColumnLink("Target", row => targetSelector(row) is { Length: > 0 } t ? LinkKey.Node(t) : null);

    public static TableOptions<T> WithColumnLink<T>(this TableOptions<T> options, string columnName, Func<T, string?> mapper)
    {
        var dict = options.ColumnLinks != null
                        ? new Dictionary<string, Func<T, string?>>(options.ColumnLinks)
                        : [];

        dict[columnName] = mapper;
        return options with { ColumnLinks = dict };
    }

    public static TableOptions<T> WithRowKeys<T>(this TableOptions<T> options, Func<T, IEnumerable<string>> rowKeys)
        => options with { RegisterRowKeys = rowKeys };
}
