/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Network;

/// <summary>
/// Flags bond interfaces with fewer than 2 slaves. A bond with a single slave
/// provides no redundancy — it's effectively a thin abstraction over the slave.
/// Mapped by ISO 27001:2022 A.8.14 and NIS2 Art. 21(c).
/// </summary>
internal sealed class BondingWithoutRedundancyCheck : IComplianceCheck
{
    public string Id => "network.bond-without-redundancy";
    public string Title => "Bond interfaces should have at least two slaves";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.NodeNetworks];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var ifaces = ctx.Get<NodeNetworkInfo>(ComplianceDataKind.NodeNetworks);

        foreach (var i in ifaces)
        {
            if (!i.Type.Equals("bond", StringComparison.OrdinalIgnoreCase)) { continue; }

            var slaveCount = string.IsNullOrWhiteSpace(i.Slaves)
                                ? 0
                                : i.Slaves.Split(' ', ',').Count(s => !string.IsNullOrWhiteSpace(s));

            if (slaveCount >= 2) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Low,
                ScopeType = "interface",
                Scope = $"{i.Node}/{i.Iface}",
                Title = "Bond without redundancy",
                Details = $"Bond '{i.Iface}' on node {i.Node} has {slaveCount} slave(s) — no link redundancy.",
                Remediation = "Add a second slave interface to the bond for failover, or remove the bond if not needed.",
            };
        }
    }
}
