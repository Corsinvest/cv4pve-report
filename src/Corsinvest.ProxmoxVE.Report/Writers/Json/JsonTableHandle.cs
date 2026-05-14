/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Writers.Json;

/// <summary>
/// Handle returned by JsonSectionWriter.AddTable so callers can later append rows
/// to an already-created table via AppendData.
/// </summary>
internal sealed record JsonTableHandle(string Title, System.Collections.IList Rows) : ITableHandle;
