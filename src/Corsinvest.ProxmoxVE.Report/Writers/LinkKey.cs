/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Writers;

/// <summary>
/// Builds the opaque link keys used by <see cref="IReportWriter.Links"/> and
/// <see cref="TableOptions{T}.RegisterRowKeys"/>. Centralised so writers and
/// section authors share the same vocabulary instead of duplicating string literals.
/// </summary>
internal static class LinkKey
{
    public static string Node(string node) => $"node:{node}";
    public static string Vm(long vmId) => $"vm:{vmId}";
    public static string Storage(string node, string storage) => $"storage:{node}:{storage}";
    public static string NodeNetwork(string node, string iface) => $"node:{node}:network:{iface}";
    public static string List(string what) => $"list:{what}";

    public const string ListNodes = "list:nodes";
    public const string ListVms = "list:vms";
    public const string ListContainers = "list:containers";

    // Section pages — auto-registered by writers via ForSection(name) in AddSection.
    public const string Cluster = "section:cluster";
    public const string ClusterAccess = "section:cluster-access";
    public const string ClusterSdn = "section:cluster-sdn";
    public const string ClusterHa = "section:cluster-ha";
    public const string ClusterPools = "section:cluster-pools";
    public const string ClusterLog = "section:cluster-log";
    public const string ClusterTasks = "section:cluster-tasks";
    public const string Storages = "section:storages";
    public const string StorageContent = "section:storage-content";
    public const string Backups = "section:backups";
    public const string Disks = "section:disks";
    public const string Partitions = "section:partitions";
    public const string Snapshots = "section:snapshots";
    public const string Network = "section:network";
    public const string Firewall = "section:firewall";
    public const string Replication = "section:replication";
    public const string RrdNodes = "section:rrd-nodes";
    public const string RrdStorage = "section:rrd-storage";
    public const string RrdGuests = "section:rrd-guests";
    public const string Syslog = "section:syslog";
    public const string Issues = "section:issues";
    public const string Compliance = "section:compliance";

    /// <summary>Hyperlink target for a specific compliance pack inside the Compliance section.</summary>
    public static string CompliancePack(string packId) => $"compliance:{packId.ToLowerInvariant()}";

    /// <summary>
    /// Returns the canonical <c>section:*</c> key for a top-level section name,
    /// or null if the name doesn't map to a known global page.
    /// </summary>
    public static string? ForSection(string sectionName)
        => sectionName switch
        {
            "Cluster" => Cluster,
            "Cluster Access" => ClusterAccess,
            "Cluster SDN" => ClusterSdn,
            "Cluster HA" => ClusterHa,
            "Cluster Pools" => ClusterPools,
            "Cluster Log" => ClusterLog,
            "Cluster Tasks" => ClusterTasks,
            "Storages" => Storages,
            "Storage Content" => StorageContent,
            "Backups" => Backups,
            "Disks" => Disks,
            "Partitions" => Partitions,
            "Snapshots" => Snapshots,
            "Network" => Network,
            "Firewall" => Firewall,
            "Replication" => Replication,
            "RRD Nodes" => RrdNodes,
            "RRD Storage" => RrdStorage,
            "RRD Guests" => RrdGuests,
            "Syslog" => Syslog,
            "Issues" => Issues,
            "Compliance" => Compliance,
            _ => null,
        };
}
