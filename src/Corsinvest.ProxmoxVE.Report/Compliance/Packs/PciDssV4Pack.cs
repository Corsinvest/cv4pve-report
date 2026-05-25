/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Checks.Access;
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.Backup;
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.Crypto;
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.Firewall;
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.Logging;
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.Network;
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.Resilience;
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.System;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Packs;

/// <summary>
/// PCI DSS v4.0 — Payment Card Industry Data Security Standard. Mapped to the
/// requirements verifiable from PVE state; many requirements (Req 3 PAN handling,
/// Req 9 physical access, Req 12 policy) are out of scope for an automated check.
/// </summary>
internal sealed class PciDssV4Pack : ICompliancePack
{
    public string Id => "PCIDSSv4";
    public string Title => "PCI DSS v4.0";

    public IReadOnlyList<IComplianceControl> Controls { get; } =
    [
        new ComplianceControl(
            Id: "Req 1",
            Title: "Install and maintain network security controls",
            Checks:
            [
                new ClusterFirewallDisabledCheck(),
                new NodeFirewallDisabledCheck(),
                new ClusterFirewallDefaultPolicyCheck(),
                new FirewallRuleAllowAnyAnyCheck(),
                new ManyDisabledFirewallRulesCheck(),
                new BondingWithoutRedundancyCheck(),
            ]),

        new ComplianceControl(
            Id: "Req 2",
            Title: "Apply secure configurations to all system components",
            Checks:
            [
                new PveVersionMismatchCheck(),
                new KernelMismatchCheck(),
                new NodesWithExpiredSubscriptionCheck(),
            ]),

        new ComplianceControl(
            Id: "Req 4",
            Title: "Protect cardholder data with strong cryptography during transmission",
            Checks:
            [
                new CertificateExpiredCheck(),
                new CertificateExpiringCheck(),
                new CertificateSelfSignedCheck(),
            ]),

        new ComplianceControl(
            Id: "Req 7",
            Title: "Restrict access by business need to know",
            Checks:
            [
                new DisabledUserStillInAdminAclCheck(),
                new MultipleAdminUsersCheck(),
                new NonPropagatedAdminAclCheck(),
                new UnusedCustomRolesCheck(),
            ]),

        new ComplianceControl(
            Id: "Req 8",
            Title: "Identify users and authenticate access",
            Checks:
            [
                new AdminWithoutTfaCheck(),
                new AdminGroupMemberWithoutTfaCheck(),
                new ApiTokenWithoutExpiryCheck(),
                new RootApiTokensWithFullPrivCheck(),
                new WeakRealmTfaCheck(),
                new UserExpiredCheck(),
            ]),

        new ComplianceControl(
            Id: "Req 10",
            Title: "Log and monitor all access",
            Checks:
            [
                new FirewallRuleNoLoggingCheck(),
                new HighTaskErrorRateCheck(),
                new ClusterLogErrorsCheck(),
                new MetricServerNotConfiguredCheck(),
                new MetricServersAllDisabledCheck(),
            ]),

        new ComplianceControl(
            Id: "Req 11",
            Title: "Test security of systems and networks regularly",
            Checks:
            [
                new VmsWithoutHaResourceCheck(),
                new HaResourcesInErrorStateCheck(),
                new OfflineNodesCheck(),
                new VmsWithoutBackupJobCheck(),
                new BackupJobsWithoutScheduleCheck(),
                new RecentBackupFailureCheck(),
            ]),
    ];
}
