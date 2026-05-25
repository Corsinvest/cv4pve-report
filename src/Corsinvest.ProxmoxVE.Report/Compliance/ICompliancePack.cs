/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance;

internal interface ICompliancePack
{
    /// <summary>Short id (es. "ISO27001", "NIS2").</summary>
    string Id { get; }

    /// <summary>Full title (es. "ISO/IEC 27001:2022").</summary>
    string Title { get; }

    IReadOnlyList<IComplianceControl> Controls { get; }
}
