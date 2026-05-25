/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance.Models;

/// <summary>
/// Cluster storage snapshot. Usage is the storage-level used-bytes / total-bytes ratio
/// (0..1), or <c>null</c> when usage is not available (e.g. unconfigured storage on a node).
/// </summary>
internal sealed record StorageInfo(string Storage,
                                   string Node,
                                   string Type,
                                   string? Content,
                                   bool Shared,
                                   bool Enabled,
                                   double? UsagePct);
