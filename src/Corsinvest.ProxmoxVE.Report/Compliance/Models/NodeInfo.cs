/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance.Models;

/// <summary>
/// Per-node snapshot for compliance checks. Subscription/version are nullable
/// because the API may be unreachable when the node is offline.
/// </summary>
internal sealed record NodeInfo(string Node,
                                bool IsOnline,
                                string? SubscriptionStatus,
                                string? SubscriptionNextDueDate,
                                string? PveVersion,
                                string? PveRelease,
                                string? KernelRelease);
