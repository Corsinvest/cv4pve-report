/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Compliance;

/// <summary>
/// Orchestrates compliance verification. Producer sections call <see cref="IsRequired"/>
/// to skip work when no enabled pack needs a data kind, then call <see cref="Provide"/>
/// to push mapped DTOs in. <see cref="Run"/> executes the checks at the end.
/// </summary>
internal sealed class ComplianceEngine
{
    private readonly IReadOnlyList<ICompliancePack> _enabledPacks;
    private readonly HashSet<ComplianceDataKind> _required;
    private readonly ComplianceContext _ctx = new();

    public ComplianceEngine(IEnumerable<ICompliancePack> enabledPacks)
    {
        _enabledPacks = [.. enabledPacks];
        _required = [.. _enabledPacks
                       .SelectMany(p => p.Controls)
                       .SelectMany(c => c.Checks)
                       .SelectMany(ch => ch.Requires)];
    }

    public bool AnyEnabled => _enabledPacks.Count > 0;

    public bool IsRequired(ComplianceDataKind kind)
        => _required.Contains(kind);

    public void Provide<T>(ComplianceDataKind kind, IReadOnlyList<T> data)
    {
        if (_required.Contains(kind)) { _ctx.Set(kind, data); }
    }

    public IReadOnlyList<ComplianceReport> Run()
        => [.. _enabledPacks.Select(RunPack)];

    private ComplianceReport RunPack(ICompliancePack pack)
    {
        var controls = pack.Controls.Select(RunControl).ToList();
        return new ComplianceReport(pack.Id, pack.Title, controls);
    }

    private ControlReport RunControl(IComplianceControl control)
    {
        var findings = new List<ComplianceFinding>();
        var skipped = new List<string>();
        var outcomes = new List<CheckOutcome>();

        foreach (var check in control.Checks)
        {
            if (_ctx.HasAll(check.Requires))
            {
                var checkFindings = check.Run(_ctx).ToList();
                findings.AddRange(checkFindings);
                outcomes.Add(new CheckOutcome(check.Id, check.Title, Executed: true, FindingCount: checkFindings.Count));
            }
            else
            {
                skipped.Add(check.Id);
                outcomes.Add(new CheckOutcome(check.Id, check.Title, Executed: false, FindingCount: 0));
            }
        }

        return new ControlReport(control.Id, control.Title, control.Checks.Count, outcomes, findings, skipped);
    }
}
