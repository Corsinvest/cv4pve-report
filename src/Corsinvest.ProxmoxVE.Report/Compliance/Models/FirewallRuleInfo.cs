/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance.Models;

/// <summary>
/// Firewall rule snapshot used by compliance checks. <see cref="ScopeType"/> is
/// "cluster" / "node" / "vm" / "ct" and <see cref="Scope"/> is the corresponding
/// identifier (node name, vmid, or empty for cluster).
/// </summary>
internal sealed record FirewallRuleInfo(string ScopeType,
                                        string Scope,
                                        string ScopeName,
                                        int Position,
                                        string Type,
                                        string Action,
                                        bool Enabled,
                                        string? Source,
                                        string? Dest,
                                        string? Macro,
                                        string? Iface,
                                        string? Log);
