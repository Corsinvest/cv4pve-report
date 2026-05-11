/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report;

internal sealed class IssueTracker
{
    private readonly List<Issue> _issues = [];

    public IReadOnlyList<Issue> All => _issues;
    public bool HasAny => _issues.Count > 0;

    public void Add(IssueSeverity severity, string section, string message, string linkKey)
        => _issues.Add(new Issue(severity, section, message, DateTime.Now, linkKey));

    public void Info(string section, string message, string linkKey) => Add(IssueSeverity.Info, section, message, linkKey);
    public void Warning(string section, string message, string linkKey) => Add(IssueSeverity.Warning, section, message, linkKey);
    public void Error(string section, string message, string linkKey) => Add(IssueSeverity.Error, section, message, linkKey);
}
