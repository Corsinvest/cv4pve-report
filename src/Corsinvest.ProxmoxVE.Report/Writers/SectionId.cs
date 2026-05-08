/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Writers;

/// <summary>
/// Identifies a section. The <see cref="Key"/> is the stable identifier used by
/// writers (sheet name in XLSX, file name in HTML) and by the cross-section link
/// table. Concrete subtypes carry the raw data (id, name) so each writer can
/// derive its own user-facing label without the caller pre-formatting strings.
/// </summary>
internal abstract record SectionId(string Key)
{
    /// <summary>Plain top-level section (Cluster, Nodes, VMs, Storages, Network, …).</summary>
    public sealed record Plain(string Name) : SectionId(Name);

    /// <summary>Per-node detail section.</summary>
    public sealed record Node(string Hostname) : SectionId($"Node {Hostname}");

    /// <summary>Per-VM (QEMU) detail section.</summary>
    public sealed record Vm(long Id, string DisplayLabel) : SectionId($"VM {Id}");

    /// <summary>Per-CT (LXC) detail section.</summary>
    public sealed record Container(long Id, string DisplayLabel) : SectionId($"CT {Id}");
}
