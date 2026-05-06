/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Writers;

/// <summary>
/// Opaque reference to a table previously created via <see cref="ISectionWriter.AddTable{T}"/>.
/// Used by <see cref="ISectionWriter.AppendData{T}"/> to add more rows.
/// </summary>
internal interface ITableHandle
{
    string Title { get; }
}
