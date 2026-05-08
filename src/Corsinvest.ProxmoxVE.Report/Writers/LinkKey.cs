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
    public static string Storage() => "storage:link";
    public static string List(string what) => $"list:{what}";
    public static string NodeNetwork(string node, string iface) => $"node:{node}:network:{iface}";

    public static string ListNodes() => List("nodes");
    public static string ListVms() => List("vms");
    public static string ListContainers() => List("containers");
}
