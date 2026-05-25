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
/// NIS2 Directive (EU 2022/2555), Article 21 — Cybersecurity risk-management measures.
/// Controls are the 10 minimum measures from Art. 21(2). Most checks are shared with
/// ISO 27001 (e.g. MFA is both ISO A.5.17 and NIS2 Art. 21(j)) — same check instances
/// referenced from both packs to avoid duplicated logic.
/// </summary>
internal sealed class Nis2Pack : ICompliancePack
{
    public string Id => "NIS2";
    public string Title => "NIS2 — Directive (EU) 2022/2555";

    public IReadOnlyList<IComplianceControl> Controls { get; } =
    [
        new ComplianceControl(
            Id: "Art.21(b)",
            Title: "Incident handling",
            Checks:
            [
                new MetricServerNotConfiguredCheck(),
                new MetricServersAllDisabledCheck(),
                new FirewallRuleNoLoggingCheck(),
                new HighTaskErrorRateCheck(),
                new ClusterLogErrorsCheck(),
            ]),

        new ComplianceControl(
            Id: "Art.21(c)",
            Title: "Business continuity, backup management, disaster recovery",
            Checks:
            [
                new VmsWithoutBackupJobCheck(),
                new BackupJobsWithoutScheduleCheck(),
                new DisabledBackupJobsCheck(),
                new RecentBackupFailureCheck(),
                new VmsWithoutHaResourceCheck(),
                new VmsWithoutReplicationCheck(),
                new HaResourcesInErrorStateCheck(),
                new OfflineNodesCheck(),
                new SingleNodeClusterCheck(),
                new BondingWithoutRedundancyCheck(),
                new HaWithoutSharedStorageCheck(),
                new StorageDisabledCheck(),
                new StorageUsageHighCheck(),
            ]),

        new ComplianceControl(
            Id: "Art.21(e)",
            Title: "Security in system acquisition, development, maintenance, vulnerability disclosure",
            Checks:
            [
                new NodesWithExpiredSubscriptionCheck(),
                new PveVersionMismatchCheck(),
                new KernelMismatchCheck(),
                new ClusterFirewallDisabledCheck(),
                new NodeFirewallDisabledCheck(),
                new ClusterFirewallDefaultPolicyCheck(),
                new FirewallRuleAllowAnyAnyCheck(),
                new ManyDisabledFirewallRulesCheck(),
            ]),

        new ComplianceControl(
            Id: "Art.21(h)",
            Title: "Policies and procedures regarding cryptography and encryption",
            Checks:
            [
                new CertificateExpiredCheck(),
                new CertificateExpiringCheck(),
                new CertificateSelfSignedCheck(),
            ]),

        new ComplianceControl(
            Id: "Art.21(i)",
            Title: "Human resources security, access control policies, asset management",
            Checks:
            [
                new UserExpiredCheck(),
                new UsersWithoutEmailCheck(),
                new RootApiTokensWithFullPrivCheck(),
                new MultipleAdminUsersCheck(),
                new DisabledUserStillInAdminAclCheck(),
                new EmptyGroupsCheck(),
                new NonPropagatedAdminAclCheck(),
                new EmptyPoolsCheck(),
                new UnusedCustomRolesCheck(),
            ]),

        new ComplianceControl(
            Id: "Art.21(j)",
            Title: "Multi-factor authentication",
            Checks:
            [
                new AdminWithoutTfaCheck(),
                new AdminGroupMemberWithoutTfaCheck(),
                new ApiTokenWithoutExpiryCheck(),
                new WeakRealmTfaCheck(),
            ]),
    ];
}
