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
/// AgID Misure Minime di Sicurezza ICT per le Pubbliche Amministrazioni
/// (circular 18/2017). Italian PA baseline; the ABSC controls map closely onto
/// CIS / ISO areas — same shared checks are reused.
/// </summary>
internal sealed class AgIdPack : ICompliancePack
{
    public string Id => "AgID";
    public string Title => "AgID Misure Minime ICT";

    public IReadOnlyList<IComplianceControl> Controls { get; } =
    [
        new ComplianceControl(
            Id: "ABSC 1",
            Title: "Inventario dispositivi autorizzati",
            Checks:
            [
                new OfflineNodesCheck(),
                new SingleNodeClusterCheck(),
            ]),

        new ComplianceControl(
            Id: "ABSC 3",
            Title: "Configurazioni sicure",
            Checks:
            [
                new PveVersionMismatchCheck(),
                new KernelMismatchCheck(),
                new NodesWithExpiredSubscriptionCheck(),
                new ClusterFirewallDefaultPolicyCheck(),
            ]),

        new ComplianceControl(
            Id: "ABSC 5",
            Title: "Uso appropriato dei privilegi di amministratore",
            Checks:
            [
                new AdminWithoutTfaCheck(),
                new AdminGroupMemberWithoutTfaCheck(),
                new ApiTokenWithoutExpiryCheck(),
                new RootApiTokensWithFullPrivCheck(),
                new WeakRealmTfaCheck(),
                new MultipleAdminUsersCheck(),
                new DisabledUserStillInAdminAclCheck(),
                new UserExpiredCheck(),
                new UsersWithoutEmailCheck(),
                new NonPropagatedAdminAclCheck(),
            ]),

        new ComplianceControl(
            Id: "ABSC 8",
            Title: "Difese contro i malware (network perimeter)",
            Checks:
            [
                new ClusterFirewallDisabledCheck(),
                new NodeFirewallDisabledCheck(),
                new FirewallRuleAllowAnyAnyCheck(),
                new ManyDisabledFirewallRulesCheck(),
            ]),

        new ComplianceControl(
            Id: "ABSC 10",
            Title: "Copie di sicurezza",
            Checks:
            [
                new VmsWithoutBackupJobCheck(),
                new BackupJobsWithoutScheduleCheck(),
                new DisabledBackupJobsCheck(),
                new RecentBackupFailureCheck(),
                new StorageDisabledCheck(),
                new StorageUsageHighCheck(),
                new VmsWithoutHaResourceCheck(),
                new VmsWithoutReplicationCheck(),
                new HaResourcesInErrorStateCheck(),
            ]),

        new ComplianceControl(
            Id: "ABSC 13",
            Title: "Protezione dei dati (crittografia)",
            Checks:
            [
                new CertificateExpiredCheck(),
                new CertificateExpiringCheck(),
                new CertificateSelfSignedCheck(),
            ]),
    ];
}
