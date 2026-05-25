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

internal sealed class Iso27001Pack : ICompliancePack
{
    public string Id => "ISO27001";
    public string Title => "ISO/IEC 27001:2022";

    public IReadOnlyList<IComplianceControl> Controls { get; } =
    [
        new ComplianceControl(
            Id: "A.5.16",
            Title: "Identity management",
            Checks:
            [
                new UserExpiredCheck(),
                new UsersWithoutEmailCheck(),
            ]),

        new ComplianceControl(
            Id: "A.5.17",
            Title: "Authentication information",
            Checks:
            [
                new AdminWithoutTfaCheck(),
                new AdminGroupMemberWithoutTfaCheck(),
                new ApiTokenWithoutExpiryCheck(),
                new RootApiTokensWithFullPrivCheck(),
                new WeakRealmTfaCheck(),
            ]),

        new ComplianceControl(
            Id: "A.5.18",
            Title: "Access rights",
            Checks:
            [
                new UserExpiredCheck(),
                new DisabledUserStillInAdminAclCheck(),
                new EmptyGroupsCheck(),
                new NonPropagatedAdminAclCheck(),
                new EmptyPoolsCheck(),
                new UnusedCustomRolesCheck(),
            ]),

        new ComplianceControl(
            Id: "A.8.2",
            Title: "Privileged access rights",
            Checks:
            [
                new RootApiTokensWithFullPrivCheck(),
                new MultipleAdminUsersCheck(),
            ]),

        new ComplianceControl(
            Id: "A.8.5",
            Title: "Secure authentication",
            Checks:
            [
                new AdminWithoutTfaCheck(),
                new AdminGroupMemberWithoutTfaCheck(),
                new WeakRealmTfaCheck(),
            ]),

        new ComplianceControl(
            Id: "A.8.6",
            Title: "Capacity management",
            Checks:
            [
                new StorageUsageHighCheck(),
            ]),

        new ComplianceControl(
            Id: "A.8.8",
            Title: "Management of technical vulnerabilities",
            Checks:
            [
                new NodesWithExpiredSubscriptionCheck(),
                new KernelMismatchCheck(),
            ]),

        new ComplianceControl(
            Id: "A.8.9",
            Title: "Configuration management",
            Checks:
            [
                new PveVersionMismatchCheck(),
                new KernelMismatchCheck(),
            ]),

        new ComplianceControl(
            Id: "A.8.13",
            Title: "Information backup",
            Checks:
            [
                new VmsWithoutBackupJobCheck(),
                new BackupJobsWithoutScheduleCheck(),
                new DisabledBackupJobsCheck(),
                new RecentBackupFailureCheck(),
                new StorageDisabledCheck(),
            ]),

        new ComplianceControl(
            Id: "A.8.14",
            Title: "Redundancy of information processing facilities",
            Checks:
            [
                new VmsWithoutHaResourceCheck(),
                new VmsWithoutReplicationCheck(),
                new HaResourcesInErrorStateCheck(),
                new OfflineNodesCheck(),
                new SingleNodeClusterCheck(),
                new BondingWithoutRedundancyCheck(),
                new HaWithoutSharedStorageCheck(),
            ]),

        new ComplianceControl(
            Id: "A.8.15",
            Title: "Logging",
            Checks:
            [
                new FirewallRuleNoLoggingCheck(),
                new HighTaskErrorRateCheck(),
                new ClusterLogErrorsCheck(),
            ]),

        new ComplianceControl(
            Id: "A.8.16",
            Title: "Monitoring activities",
            Checks:
            [
                new MetricServerNotConfiguredCheck(),
                new MetricServersAllDisabledCheck(),
            ]),

        new ComplianceControl(
            Id: "A.8.20",
            Title: "Networks security",
            Checks:
            [
                new ClusterFirewallDisabledCheck(),
                new NodeFirewallDisabledCheck(),
            ]),

        new ComplianceControl(
            Id: "A.8.21",
            Title: "Security of network services",
            Checks:
            [
                new ClusterFirewallDefaultPolicyCheck(),
                new FirewallRuleAllowAnyAnyCheck(),
                new ManyDisabledFirewallRulesCheck(),
            ]),

        new ComplianceControl(
            Id: "A.8.24",
            Title: "Use of cryptography",
            Checks:
            [
                new CertificateExpiredCheck(),
                new CertificateExpiringCheck(),
                new CertificateSelfSignedCheck(),
            ]),
    ];
}
