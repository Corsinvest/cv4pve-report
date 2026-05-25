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
/// NIST Cybersecurity Framework 2.0 (February 2024) — six functions:
/// Govern, Identify, Protect, Detect, Respond, Recover. Govern is policy-only
/// (out of scope for automated checks) so it's omitted.
/// </summary>
internal sealed class NistCsfV2Pack : ICompliancePack
{
    public string Id => "NISTCSFv2";
    public string Title => "NIST Cybersecurity Framework 2.0";

    public IReadOnlyList<IComplianceControl> Controls { get; } =
    [
        new ComplianceControl(
            Id: "ID.AM",
            Title: "Identify — Asset Management",
            Checks:
            [
                new OfflineNodesCheck(),
                new SingleNodeClusterCheck(),
                new PveVersionMismatchCheck(),
                new KernelMismatchCheck(),
            ]),

        new ComplianceControl(
            Id: "ID.RA",
            Title: "Identify — Risk Assessment",
            Checks:
            [
                new NodesWithExpiredSubscriptionCheck(),
                new CertificateExpiringCheck(),
                new StorageUsageHighCheck(),
            ]),

        new ComplianceControl(
            Id: "PR.AA",
            Title: "Protect — Identity Management, Authentication, Access Control",
            Checks:
            [
                new AdminWithoutTfaCheck(),
                new AdminGroupMemberWithoutTfaCheck(),
                new ApiTokenWithoutExpiryCheck(),
                new RootApiTokensWithFullPrivCheck(),
                new WeakRealmTfaCheck(),
                new UserExpiredCheck(),
                new DisabledUserStillInAdminAclCheck(),
                new MultipleAdminUsersCheck(),
                new UsersWithoutEmailCheck(),
                new NonPropagatedAdminAclCheck(),
                new EmptyGroupsCheck(),
                new EmptyPoolsCheck(),
                new UnusedCustomRolesCheck(),
            ]),

        new ComplianceControl(
            Id: "PR.DS",
            Title: "Protect — Data Security",
            Checks:
            [
                new CertificateExpiredCheck(),
                new CertificateExpiringCheck(),
                new CertificateSelfSignedCheck(),
            ]),

        new ComplianceControl(
            Id: "PR.IR",
            Title: "Protect — Platform Security / Resilience",
            Checks:
            [
                new ClusterFirewallDisabledCheck(),
                new NodeFirewallDisabledCheck(),
                new ClusterFirewallDefaultPolicyCheck(),
                new FirewallRuleAllowAnyAnyCheck(),
                new ManyDisabledFirewallRulesCheck(),
                new BondingWithoutRedundancyCheck(),
                new PveVersionMismatchCheck(),
                new KernelMismatchCheck(),
                new NodesWithExpiredSubscriptionCheck(),
            ]),

        new ComplianceControl(
            Id: "DE.CM",
            Title: "Detect — Continuous Monitoring",
            Checks:
            [
                new FirewallRuleNoLoggingCheck(),
                new HighTaskErrorRateCheck(),
                new ClusterLogErrorsCheck(),
                new MetricServerNotConfiguredCheck(),
                new MetricServersAllDisabledCheck(),
            ]),

        new ComplianceControl(
            Id: "RS.MA",
            Title: "Respond — Incident Management",
            Checks:
            [
                new HaResourcesInErrorStateCheck(),
                new OfflineNodesCheck(),
            ]),

        new ComplianceControl(
            Id: "RC.RP",
            Title: "Recover — Recovery Plan Execution",
            Checks:
            [
                new VmsWithoutBackupJobCheck(),
                new BackupJobsWithoutScheduleCheck(),
                new DisabledBackupJobsCheck(),
                new RecentBackupFailureCheck(),
                new VmsWithoutHaResourceCheck(),
                new VmsWithoutReplicationCheck(),
                new HaWithoutSharedStorageCheck(),
                new StorageDisabledCheck(),
                new StorageUsageHighCheck(),
                new SingleNodeClusterCheck(),
            ]),
    ];
}
