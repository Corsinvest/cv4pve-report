/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;

namespace Corsinvest.ProxmoxVE.Report;

internal class ProgressTracker(IProgress<ReportProgress>? progress, int total)
{
    private int _current;
    private ClusterResource? _resource;

    public void Next(ClusterResource resource)
    {
        _current++;
        _resource = resource;
        progress?.Report(new ReportProgress { Resource = resource, Current = _current, Total = total });
    }

    public void Step(string step)
        => progress?.Report(new ReportProgress { Resource = _resource, Current = _current, Total = total, Step = step });
}
