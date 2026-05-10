/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace Corsinvest.ProxmoxVE.Report.Writers.Html;

internal sealed partial class HtmlReportWriter
{
    public void WriteCoverPage(Settings settings, IEnumerable<SectionStat> stats)
    {
        var statsList = stats.ToList();
        var totalDuration = TimeSpan.FromSeconds(statsList.Sum(s => s.Duration.TotalSeconds));

        var rows = string.Concat(statsList.Select(s => $"""
                  <tr>
                    <td><a href="{HtmlEncoder.PageHref(s.Name)}">{HtmlEncoder.Text(s.Name)}</a></td>
                    <td class="num">{s.Count}</td>
                    <td class="num">{FormatDuration(s.Duration)}</td>
                  </tr>

            """));

        var filters = string.Concat(BuildFilters(settings).Select(f => $"""
                      <tr><th scope="row">{HtmlEncoder.Text(f.Key)}</th><td>{HtmlEncoder.Text(f.Value)}</td></tr>

            """));

        _coverHtml = $$"""
                  <section class="cover-info">
                    <h2>Report Information</h2>
                    <table class="kv">
                      <tbody>
                        <tr><th scope="row">Generated</th><td>{{DateTime.Now:yyyy-MM-dd HH:mm:ss}}</td></tr>
                        <tr><th scope="row">Application</th><td>{{HtmlEncoder.Text(_info.ApplicationName)}} v{{HtmlEncoder.Text(_info.ApplicationVersion)}}</td></tr>
                      </tbody>
                    </table>
                  </section>

                  <section class="cover-filters">
                    <h2>Filters Applied</h2>
                    <table class="kv">
                      <tbody>
            {{filters}}      </tbody>
                    </table>
                  </section>

                  <section class="cover-contents">
                    <h2>Contents</h2>
                    <div class="table-scroll">
                      <table class="data">
                        <thead><tr><th>Section</th><th>Rows</th><th>Duration</th></tr></thead>
                        <tbody>
            {{rows}}      </tbody>
                        <tfoot>
                          <tr><th>Total</th><th class="num"></th><th class="num">{{FormatDuration(totalDuration)}}</th></tr>
                        </tfoot>
                      </table>
                    </div>
                  </section>

            """;
    }

    private static string FormatDuration(TimeSpan d)
        => d.TotalSeconds < 60 ? $"{d.TotalSeconds:F1}s" : $"{d.TotalMinutes:F1}m";

    private static IEnumerable<KeyValuePair<string, string>> BuildFilters(Settings settings)
    {
        yield return new("Nodes", settings.Node.Names ?? "");
        yield return new("VMs/Containers", settings.Guest.Ids ?? "");

        if (settings.Node.RrdData.Enabled)
        {
            yield return new("Node RRD TimeFrame", settings.Node.RrdData.TimeFrame.ToString());
            yield return new("Node RRD Consolidation", settings.Node.RrdData.Consolidation.ToString());
        }

        if (settings.Guest.RrdData.Enabled)
        {
            yield return new("Guest RRD TimeFrame", settings.Guest.RrdData.TimeFrame.ToString());
            yield return new("Guest RRD Consolidation", settings.Guest.RrdData.Consolidation.ToString());
        }

        if (settings.Storage.RrdData.Enabled)
        {
            yield return new("Storage RRD TimeFrame", settings.Storage.RrdData.TimeFrame.ToString());
            yield return new("Storage RRD Consolidation", settings.Storage.RrdData.Consolidation.ToString());
        }
    }
}
