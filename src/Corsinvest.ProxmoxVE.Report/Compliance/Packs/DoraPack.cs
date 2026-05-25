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
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.Storage;
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.System;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Packs;

/// <summary>
/// DORA — Digital Operational Resilience Act (EU 2022/2554). ICT risk management
/// for the financial sector. Mapped to chapters II (ICT risk management) and IV
/// (digital operational resilience testing) for the parts verifiable from PVE state.
/// </summary>
internal sealed class DoraPack : ICompliancePack
{
    public string Id => "DORA";
    public string Title => "Regulation (EU) 2022/2554";

    public IReadOnlyList<IComplianceControl> Controls { get; } =
    [
        new ComplianceControl(
            Id: "Art.5",
            Title: "ICT risk management framework",
            Checks:
            [
                new MetricServerNotConfiguredCheck(),
                new MetricServersAllDisabledCheck(),
                new HighTaskErrorRateCheck(),
                new ClusterLogErrorsCheck(),
                new OfflineNodesCheck(),
                new HaResourcesInErrorStateCheck(),
            ]),

        new ComplianceControl(
            Id: "Art.8",
            Title: "Identification (ICT systems and assets)",
            Checks:
            [
                new SingleNodeClusterCheck(),
                new OfflineNodesCheck(),
                new PveVersionMismatchCheck(),
                new KernelMismatchCheck(),
            ]),

        new ComplianceControl(
            Id: "Art.9",
            Title: "Protection and prevention",
            Checks:
            [
                new ClusterFirewallDisabledCheck(),
                new NodeFirewallDisabledCheck(),
                new ClusterFirewallDefaultPolicyCheck(),
                new FirewallRuleAllowAnyAnyCheck(),
                new ManyDisabledFirewallRulesCheck(),
                new CertificateExpiredCheck(),
                new CertificateExpiringCheck(),
                new CertificateSelfSignedCheck(),
                new AdminWithoutTfaCheck(),
                new AdminGroupMemberWithoutTfaCheck(),
                new ApiTokenWithoutExpiryCheck(),
                new RootApiTokensWithFullPrivCheck(),
                new WeakRealmTfaCheck(),
                new UserExpiredCheck(),
                new DisabledUserStillInAdminAclCheck(),
                new MultipleAdminUsersCheck(),
                new NodesWithExpiredSubscriptionCheck(),
                new BondingWithoutRedundancyCheck(),
            ]),

        new ComplianceControl(
            Id: "Art.10",
            Title: "Detection",
            Checks:
            [
                new FirewallRuleNoLoggingCheck(),
                new HighTaskErrorRateCheck(),
                new ClusterLogErrorsCheck(),
                new MetricServerNotConfiguredCheck(),
                new MetricServersAllDisabledCheck(),
            ]),

        new ComplianceControl(
            Id: "Art.11",
            Title: "Response and recovery (business continuity and DR)",
            Checks:
            [
                new VmsWithoutBackupJobCheck(),
                new BackupJobsWithoutScheduleCheck(),
                new DisabledBackupJobsCheck(),
                new RecentBackupFailureCheck(),
                new VmsWithoutHaResourceCheck(),
                new VmsWithoutReplicationCheck(),
                new HaResourcesInErrorStateCheck(),
                new HaWithoutSharedStorageCheck(),
                new StorageDisabledCheck(),
                new StorageUsageHighCheck(),
                new OfflineNodesCheck(),
                new SingleNodeClusterCheck(),
            ]),
    ];
}
