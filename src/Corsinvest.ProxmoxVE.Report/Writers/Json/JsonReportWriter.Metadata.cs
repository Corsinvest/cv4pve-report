/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Writers.Json;

internal sealed partial class JsonReportWriter
{
    /// <summary>
    /// Builds the <c>metadata.json</c> payload — the JSON equivalent of the
    /// XLSX "Summary" sheet and the HTML <c>index.html</c> cover page. Carries
    /// the schema version, generation timestamp, application info, the same
    /// subset of settings the Excel/HTML covers expose (node/guest filters and
    /// RRD timeframes when enabled) and the per-section generation stats.
    /// </summary>
    private object BuildMetadata()
        => new
        {
            schemaVersion = 1,
            generatedAt = _generatedAt.ToString("O"),
            applicationName = _info.ApplicationName,
            applicationVersion = _info.ApplicationVersion,
            applicationUrl = _info.ApplicationUrl,
            filters = _settings is null ? null : new
            {
                nodes = _settings.Node.Names,
                guests = _settings.Guest.Ids,
                nodeRrd = _settings.Node.RrdData.Enabled
                            ? new
                            {
                                timeFrame = _settings.Node.RrdData.TimeFrame.ToString(),
                                consolidation = _settings.Node.RrdData.Consolidation.ToString(),
                            }
                            : null,
                guestRrd = _settings.Guest.RrdData.Enabled
                            ? new
                            {
                                timeFrame = _settings.Guest.RrdData.TimeFrame.ToString(),
                                consolidation = _settings.Guest.RrdData.Consolidation.ToString(),
                            }
                            : null,
                storageRrd = _settings.Storage.RrdData.Enabled
                            ? new
                            {
                                timeFrame = _settings.Storage.RrdData.TimeFrame.ToString(),
                                consolidation = _settings.Storage.RrdData.Consolidation.ToString(),
                            }
                            : null,
            },
            sections = _stats.Select(s => new
            {
                name = s.Name,
                count = s.Count,
                durationSeconds = s.Duration.TotalSeconds,
            }),
        };
}
