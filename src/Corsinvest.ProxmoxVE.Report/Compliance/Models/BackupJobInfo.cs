/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance.Models;

/// <summary>
/// Snapshot of a cluster backup job (vzdump scheduled). Retention/prune is
/// out of scope: managed downstream by Proxmox Backup Server and not exposed
/// in the PVE Cluster.Backup API.
/// </summary>
internal sealed record BackupJobInfo(string Id,
                                     bool Enabled,
                                     bool All,
                                     IReadOnlyList<long> VmIds,
                                     string? Pool,
                                     string? Node,
                                     string? Storage,
                                     string? Schedule);
