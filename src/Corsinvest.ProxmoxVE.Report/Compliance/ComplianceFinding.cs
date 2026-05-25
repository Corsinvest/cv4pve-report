/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance;

internal sealed record ComplianceFinding
{
    public required string CheckId { get; init; }
    public required Severity Severity { get; init; }
    public required string ScopeType { get; init; }
    public required string Scope { get; init; }
    public string ScopeName { get; init; } = "";
    public required string Title { get; init; }
    public string Details { get; init; } = "";
    public string Remediation { get; init; } = "";
}
