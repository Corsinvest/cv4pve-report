/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance;

/// <summary>
/// Typed bag of data populated by producer sections and consumed by checks.
/// A check that needs <see cref="ComplianceDataKind.Users"/> can only run if
/// <see cref="Has"/> returns true for that kind — otherwise it's skipped.
/// </summary>
internal sealed class ComplianceContext
{
    private readonly Dictionary<ComplianceDataKind, object> _data = [];

    public void Set<T>(ComplianceDataKind kind, IReadOnlyList<T> data)
        => _data[kind] = data;

    public IReadOnlyList<T> Get<T>(ComplianceDataKind kind)
        => (IReadOnlyList<T>)_data[kind];

    public bool Has(ComplianceDataKind kind)
        => _data.ContainsKey(kind);

    public bool HasAll(IEnumerable<ComplianceDataKind> kinds)
        => kinds.All(Has);
}
