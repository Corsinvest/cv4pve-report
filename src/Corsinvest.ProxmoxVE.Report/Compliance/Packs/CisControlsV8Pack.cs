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
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.Storage;
using Corsinvest.ProxmoxVE.Report.Compliance.Checks.System;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Packs;

/// <summary>
/// CIS Critical Security Controls v8 — technical, prescriptive controls maintained by the
/// Center for Internet Security. Mapped to the safeguards verifiable from PVE state.
/// </summary>
internal sealed class CisControlsV8Pack : ICompliancePack
{
    public string Id => "CISv8";
    public string Title => "CIS Controls v8";

    public IReadOnlyList<IComplianceControl> Controls { get; } =
    [
        new ComplianceControl(
            Id: "CIS-3",
            Title: "Data protection",
            Checks:
            [
                new VmsWithoutBackupJobCheck(),
                new BackupJobsWithoutScheduleCheck(),
                new RecentBackupFailureCheck(),
                new CertificateExpiredCheck(),
                new CertificateExpiringCheck(),
                new CertificateSelfSignedCheck(),
            ]),

        new ComplianceControl(
            Id: "CIS-4",
            Title: "Secure configuration of enterprise assets and software",
            Checks:
            [
                new PveVersionMismatchCheck(),
                new KernelMismatchCheck(),
                new ClusterFirewallDefaultPolicyCheck(),
            ]),

        new ComplianceControl(
            Id: "CIS-5",
            Title: "Account management",
            Checks:
            [
                new UserExpiredCheck(),
                new DisabledUserStillInAdminAclCheck(),
                new UsersWithoutEmailCheck(),
                new EmptyGroupsCheck(),
                new EmptyPoolsCheck(),
                new UnusedCustomRolesCheck(),
            ]),

        new ComplianceControl(
            Id: "CIS-6",
            Title: "Access control management",
            Checks:
            [
                new AdminWithoutTfaCheck(),
                new AdminGroupMemberWithoutTfaCheck(),
                new ApiTokenWithoutExpiryCheck(),
                new RootApiTokensWithFullPrivCheck(),
                new WeakRealmTfaCheck(),
                new MultipleAdminUsersCheck(),
                new NonPropagatedAdminAclCheck(),
            ]),

        new ComplianceControl(
            Id: "CIS-7",
            Title: "Continuous vulnerability management",
            Checks:
            [
                new NodesWithExpiredSubscriptionCheck(),
                new KernelMismatchCheck(),
            ]),

        new ComplianceControl(
            Id: "CIS-8",
            Title: "Audit log management",
            Checks:
            [
                new FirewallRuleNoLoggingCheck(),
                new HighTaskErrorRateCheck(),
                new ClusterLogErrorsCheck(),
                new MetricServerNotConfiguredCheck(),
                new MetricServersAllDisabledCheck(),
            ]),

        new ComplianceControl(
            Id: "CIS-11",
            Title: "Data recovery",
            Checks:
            [
                new VmsWithoutBackupJobCheck(),
                new BackupJobsWithoutScheduleCheck(),
                new DisabledBackupJobsCheck(),
                new RecentBackupFailureCheck(),
                new StorageDisabledCheck(),
            ]),

        new ComplianceControl(
            Id: "CIS-12",
            Title: "Network infrastructure management",
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
            Id: "CIS-13",
            Title: "Network monitoring and defense",
            Checks:
            [
                new FirewallRuleNoLoggingCheck(),
                new MetricServerNotConfiguredCheck(),
            ]),
    ];
}
