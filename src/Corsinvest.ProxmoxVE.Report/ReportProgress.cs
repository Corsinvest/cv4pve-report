/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;

namespace Corsinvest.ProxmoxVE.Report;

/// <summary>
/// Progress information reported during report generation.
/// </summary>
public class ReportProgress
{
    /// <summary>
    /// The cluster resource being processed. Null for global phases (init, cluster).
    /// </summary>
    public ClusterResource? Resource { get; init; }

    /// <summary>
    /// Current sub-step within a resource (e.g. "RRD", "Smart", "QemuAgent").
    /// Null when starting a new resource.
    /// </summary>
    public string? Step { get; init; }

    /// <summary>
    /// Current index (1-based) within the current resource type.
    /// </summary>
    public int Current { get; init; }

    /// <summary>
    /// Total count for the current resource type.
    /// </summary>
    public int Total { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (Resource == null)
        {
            return string.IsNullOrEmpty(Step)
                ? ""
                : $"[{Step}]";
        }
        else
        {
            var type = Resource.ResourceType.ToString().ToLowerInvariant();

            var name = Resource.ResourceType switch
            {
                ClusterResourceType.Unknown => "Unknown",
                ClusterResourceType.Node => Resource.Node,
                ClusterResourceType.Vm => $"{Resource.VmId} {Resource.Name}",
                ClusterResourceType.Storage => $"{Resource.Node}/{Resource.Storage}",
                ClusterResourceType.Pool => Resource.Pool,
                ClusterResourceType.Sdn => Resource.Sdn,
                ClusterResourceType.All => "Unknown",
                _ => "Unknown",
            };

            var prefix = $"[{type} {Current}/{Total}] {name}";
            return string.IsNullOrEmpty(Step)
                    ? prefix
                    : $"{prefix} > {Step}";
        }
    }
}
