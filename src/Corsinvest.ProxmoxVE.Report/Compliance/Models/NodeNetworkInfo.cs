/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance.Models;

/// <summary>
/// Per-node network interface snapshot. <see cref="Slaves"/> is comma-separated when
/// <see cref="Type"/> is "bond" and lists the slave interfaces; empty otherwise.
/// </summary>
internal sealed record NodeNetworkInfo(string Node,
                                       string Iface,
                                       string Type,
                                       string? Slaves);
