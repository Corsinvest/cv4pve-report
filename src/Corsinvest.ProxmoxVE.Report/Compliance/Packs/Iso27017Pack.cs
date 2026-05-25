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
/// ISO/IEC 27017:2015 — Code of practice for information security controls based
/// on ISO/IEC 27002 for cloud services. Extends ISO 27001 with cloud-specific
/// guidance (CLD.6.3, CLD.8.1, CLD.9.5, CLD.12.1, CLD.12.4, CLD.13.1).
/// Only the cloud-extensions are listed here — ISO 27001 base controls are
/// covered by <see cref="Iso27001Pack"/>.
/// </summary>
internal sealed class Iso27017Pack : ICompliancePack
{
    public string Id => "ISO27017";
    public string Title => "ISO/IEC 27017:2015 — Cloud Security Extensions";

    public IReadOnlyList<IComplianceControl> Controls { get; } =
    [
        new ComplianceControl(
            Id: "CLD.6.3",
            Title: "Shared roles and responsibilities within a cloud computing environment",
            Checks:
            [
                new MultipleAdminUsersCheck(),
                new RootApiTokensWithFullPrivCheck(),
                new NonPropagatedAdminAclCheck(),
                new UnusedCustomRolesCheck(),
            ]),

        new ComplianceControl(
            Id: "CLD.8.1",
            Title: "Asset management — removal of cloud customer assets",
            Checks:
            [
                new EmptyPoolsCheck(),
                new EmptyGroupsCheck(),
                new DisabledUserStillInAdminAclCheck(),
                new UserExpiredCheck(),
                new StorageDisabledCheck(),
            ]),

        new ComplianceControl(
            Id: "CLD.9.5",
            Title: "Segregation in virtual computing environments",
            Checks:
            [
                new ClusterFirewallDisabledCheck(),
                new NodeFirewallDisabledCheck(),
                new ClusterFirewallDefaultPolicyCheck(),
                new FirewallRuleAllowAnyAnyCheck(),
            ]),

        new ComplianceControl(
            Id: "CLD.12.1",
            Title: "Operational procedures — administrator's operational security",
            Checks:
            [
                new AdminWithoutTfaCheck(),
                new AdminGroupMemberWithoutTfaCheck(),
                new ApiTokenWithoutExpiryCheck(),
                new WeakRealmTfaCheck(),
                new NodesWithExpiredSubscriptionCheck(),
                new PveVersionMismatchCheck(),
                new KernelMismatchCheck(),
            ]),

        new ComplianceControl(
            Id: "CLD.12.4",
            Title: "Logging — cloud service monitoring",
            Checks:
            [
                new FirewallRuleNoLoggingCheck(),
                new HighTaskErrorRateCheck(),
                new ClusterLogErrorsCheck(),
                new MetricServerNotConfiguredCheck(),
                new MetricServersAllDisabledCheck(),
            ]),

        new ComplianceControl(
            Id: "CLD.13.1",
            Title: "Alignment of security management for virtual and physical networks",
            Checks:
            [
                new ClusterFirewallDisabledCheck(),
                new NodeFirewallDisabledCheck(),
                new FirewallRuleAllowAnyAnyCheck(),
                new CertificateExpiredCheck(),
                new CertificateExpiringCheck(),
                new CertificateSelfSignedCheck(),
            ]),

        new ComplianceControl(
            Id: "ext.BC",
            Title: "Cloud-resident workload business continuity",
            Checks:
            [
                new VmsWithoutBackupJobCheck(),
                new BackupJobsWithoutScheduleCheck(),
                new RecentBackupFailureCheck(),
                new VmsWithoutHaResourceCheck(),
                new VmsWithoutReplicationCheck(),
                new HaResourcesInErrorStateCheck(),
                new HaWithoutSharedStorageCheck(),
                new StorageUsageHighCheck(),
                new OfflineNodesCheck(),
                new SingleNodeClusterCheck(),
            ]),
    ];
}
