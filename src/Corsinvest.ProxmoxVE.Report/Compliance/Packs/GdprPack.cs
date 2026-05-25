/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Checks.Access;
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.Backup;
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.Crypto;
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.Firewall;
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.Logging;
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.Resilience;
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.Storage;
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.System;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Packs;

/// <summary>
/// GDPR Art. 32 — Security of processing. Maps the verifiable technical and
/// organisational measures required to ensure a level of security appropriate
/// to the risk. Procedural Art. 30/33/35 controls are out of scope.
/// </summary>
internal sealed class GdprPack : ICompliancePack
{
    public string Id => "GDPR";
    public string Title => "GDPR — Art. 32 Security of processing";

    public IReadOnlyList<IComplianceControl> Controls { get; } =
    [
        new ComplianceControl(
            Id: "Art.32(1)(a)",
            Title: "Pseudonymisation and encryption of personal data",
            Checks:
            [
                new CertificateExpiredCheck(),
                new CertificateExpiringCheck(),
                new CertificateSelfSignedCheck(),
            ]),

        new ComplianceControl(
            Id: "Art.32(1)(b)",
            Title: "Ensure ongoing confidentiality, integrity, availability and resilience",
            Checks:
            [
                new ClusterFirewallDisabledCheck(),
                new NodeFirewallDisabledCheck(),
                new ClusterFirewallDefaultPolicyCheck(),
                new FirewallRuleAllowAnyAnyCheck(),
                new AdminWithoutTfaCheck(),
                new AdminGroupMemberWithoutTfaCheck(),
                new ApiTokenWithoutExpiryCheck(),
                new RootApiTokensWithFullPrivCheck(),
                new WeakRealmTfaCheck(),
                new UserExpiredCheck(),
                new DisabledUserStillInAdminAclCheck(),
                new MultipleAdminUsersCheck(),
                new VmsWithoutHaResourceCheck(),
                new HaResourcesInErrorStateCheck(),
                new OfflineNodesCheck(),
            ]),

        new ComplianceControl(
            Id: "Art.32(1)(c)",
            Title: "Restore availability and access to personal data in a timely manner",
            Checks:
            [
                new VmsWithoutBackupJobCheck(),
                new BackupJobsWithoutScheduleCheck(),
                new DisabledBackupJobsCheck(),
                new RecentBackupFailureCheck(),
                new VmsWithoutReplicationCheck(),
                new StorageDisabledCheck(),
                new StorageUsageHighCheck(),
            ]),

        new ComplianceControl(
            Id: "Art.32(1)(d)",
            Title: "Regular testing, assessing and evaluating effectiveness",
            Checks:
            [
                new NodesWithExpiredSubscriptionCheck(),
                new PveVersionMismatchCheck(),
                new KernelMismatchCheck(),
                new FirewallRuleNoLoggingCheck(),
                new HighTaskErrorRateCheck(),
                new ClusterLogErrorsCheck(),
                new MetricServerNotConfiguredCheck(),
                new MetricServersAllDisabledCheck(),
            ]),
    ];
}
