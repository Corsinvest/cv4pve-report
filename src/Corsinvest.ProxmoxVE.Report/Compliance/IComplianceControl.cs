/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance;

internal interface IComplianceControl
{
    /// <summary>Control id from the standard (es. "A.5.17", "Art.21(j)").</summary>
    string Id { get; }

    string Title { get; }

    IReadOnlyList<IComplianceCheck> Checks { get; }
}
