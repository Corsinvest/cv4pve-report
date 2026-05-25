/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance;

internal sealed record ComplianceReport(string PackId,
                                        string PackTitle,
                                        IReadOnlyList<ControlReport> Controls);

internal sealed record ControlReport(string ControlId,
                                     string Title,
                                     int TotalChecks,
                                     IReadOnlyList<CheckOutcome> Outcomes,
                                     IReadOnlyList<ComplianceFinding> Findings,
                                     IReadOnlyList<string> SkippedCheckIds);

/// <summary>Per-check execution result inside a control. Preserves PASS/FAIL/N/A even when no findings emitted.</summary>
internal sealed record CheckOutcome(string CheckId,
                                    string Title,
                                    bool Executed,
                                    int FindingCount);
