/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance;

/// <summary>Simple value-based <see cref="IComplianceControl"/> for declarative pack definitions.</summary>
internal sealed record ComplianceControl(string Id,
                                         string Title,
                                         IReadOnlyList<IComplianceCheck> Checks) : IComplianceControl;
