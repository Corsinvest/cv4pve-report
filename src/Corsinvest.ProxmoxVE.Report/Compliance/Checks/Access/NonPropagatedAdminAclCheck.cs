/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Report.Compliance.Models;

namespace Corsinvest.ProxmoxVE.Report.Compliance.Checks.Access;

/// <summary>
/// Flags Administrator/PVEAdmin ACL entries on "/" with Propagate=false. The
/// Administrator role on the root path is normally propagated; a non-propagated
/// entry is unusual and might indicate a misconfiguration that limits its
/// effectiveness — worth surfacing for auditor review.
/// Mapped by ISO 27001:2022 A.5.18.
/// </summary>
internal sealed class NonPropagatedAdminAclCheck : IComplianceCheck
{
    private static readonly string[] _privilegedRoles = ["Administrator", "PVEAdmin"];

    public string Id => "access.admin-acl-not-propagated";
    public string Title => "Administrator ACLs on root path should propagate";

    public IReadOnlyList<ComplianceDataKind> Requires => [ComplianceDataKind.Acl];

    public IEnumerable<ComplianceFinding> Run(ComplianceContext ctx)
    {
        var acl = ctx.Get<AclEntry>(ComplianceDataKind.Acl);

        foreach (var a in acl)
        {
            if (a.Path != "/") { continue; }
            if (!_privilegedRoles.Contains(a.RoleId)) { continue; }
            if (a.Propagate) { continue; }

            yield return new ComplianceFinding
            {
                CheckId = Id,
                Severity = Severity.Low,
                ScopeType = a.Type,
                Scope = a.UserOrGroup,
                Title = "Admin ACL on '/' without Propagate",
                Details = $"{a.Type} '{a.UserOrGroup}' has role '{a.RoleId}' on '/' but Propagate is disabled — unusual configuration.",
                Remediation = "Review the ACL: enable Propagate, or remove the entry if not intended.",
            };
        }
    }
}
