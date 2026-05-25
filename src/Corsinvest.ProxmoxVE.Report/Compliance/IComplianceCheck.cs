/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance;

internal interface IComplianceCheck
{
    /// <summary>Stable unique id (es. "ISO27001.A.5.17.admin-no-tfa"). Never change once shipped.</summary>
    string Id { get; }

    string Title { get; }

    /// <summary>Data kinds this check reads. Used to skip the check when data is unavailable.</summary>
    IReadOnlyList<ComplianceDataKind> Requires { get; }

    IEnumerable<ComplianceFinding> Run(ComplianceContext ctx);
}
