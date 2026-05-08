/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.Json;
using Corsinvest.ProxmoxVE.Api.Console.Helpers;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Report;
using Microsoft.Extensions.Logging;

const string settingsFileName = "settings.json";

var app = ConsoleHelper.CreateApp("Report for Proxmox VE");

var logLevel = app.DebugIsActive()
                ? LogLevel.Debug
                : LogLevel.Warning;

var loggerFactory = ConsoleHelper.CreateLoggerFactory<Program>(logLevel);  //(app.GetLogLevelFromDebug());
var logger = loggerFactory.CreateLogger<Program>();

var optSettingsFile = app.AddOption<string>("--settings-file", $"Settings file (default: {settingsFileName})")
                         .AddValidatorExistFile();

var cmdCreateSettings = app.AddCommand("create-settings", $"Create settings file ({settingsFileName})");
var optCreateFast = cmdCreateSettings.AddOption<bool>("--fast", "Use fast profile (structure only, no heavy data)");
var optCreateFull = cmdCreateSettings.AddOption<bool>("--full", "Use full profile (everything enabled, RRD on week timeframe)");

cmdCreateSettings.SetAction((action) =>
{
    var settings = action.GetValue(optCreateFast)
                     ? Settings.Fast()
                     : action.GetValue(optCreateFull)
                         ? Settings.Full()
                         : Settings.Standard();

    File.WriteAllText(settingsFileName, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    Console.Out.WriteLine(PrintEnum("RrdDataTimeFrame", typeof(RrdDataTimeFrame)));
    Console.Out.WriteLine(PrintEnum("RrdDataConsolidation", typeof(RrdDataConsolidation)));
    Console.Out.WriteLine($"Created: {settingsFileName}");
});

var cmdExport = app.AddCommand("export", "Generate report");
var optExportFast = cmdExport.AddOption<bool>("--fast", "Use fast profile (structure only, no heavy data)");
var optExportFull = cmdExport.AddOption<bool>("--full", "Use full profile (everything enabled, RRD on week timeframe)");
var optExportFormat = cmdExport.AddOption<ReportFormat>("--format", "Output format (default: Xlsx)");
var optOutput = cmdExport.AddOption<string>("--output|-o", "Output file path (default: Report_YYYYMMDD_HHmmss.<ext> in current directory)");
cmdExport.SetAction(async (action) =>
{
    var client = await app.ClientTryLoginAsync(loggerFactory);
    var settingsFile = action.GetValue(optSettingsFile);
    var settings = !string.IsNullOrWhiteSpace(settingsFile)
                        ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsFile))!
                        : action.GetValue(optExportFast)
                            ? Settings.Fast()
                            : action.GetValue(optExportFull)
                                ? Settings.Full()
                                : Settings.Standard();

    var engine = new ReportEngine(client, settings, new());
    var progressLock = new object();
    var progress = new Progress<ReportProgress>(p =>
    {
        if (Console.IsOutputRedirected)
        {
            Console.Out.WriteLine(p);
            return;
        }

        // Lock the writes — Progress<T>.Report() can be invoked from multiple
        // threads concurrently and otherwise produces overlapping output on the line.
        lock (progressLock)
        {
            // \x1b[2K = ANSI "erase entire line"; \r returns the cursor to column 0.
            // Together they clear residue from any previous, longer message before
            // writing the new one — no need to track length manually.
            Console.Write($"\r\x1b[2K{p}");
        }
    });

    var format = action.GetValue(optExportFormat);
    var ext = ExtensionFor(format);

    var output = action.GetValue(optOutput);
    var outputPath = !string.IsNullOrWhiteSpace(output)
                         ? output
                         : Path.Combine(".", $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}");

    await using var stream = await engine.GenerateAsync(format, progress);
    await using var file = File.Create(outputPath);
    await stream.CopyToAsync(file);

    if (!Console.IsOutputRedirected) { Console.Write("\r\x1b[2K"); }

    // For XLSX the SVG is written next to the workbook; for HTML it's already
    // bundled inside the .zip by the writer.
    if (format == ReportFormat.Xlsx && engine.NetworkDiagramSvg is { } svg)
    {
        var svgPath = Path.ChangeExtension(outputPath, ".svg");
        await File.WriteAllTextAsync(svgPath, svg);
        Console.Out.WriteLine($"Network diagram SVG: {svgPath}");
    }

    Console.Out.WriteLine($"Report generated: {outputPath}");
});

return await app.ExecuteAppAsync(args, logger);

static string PrintEnum(string title, Type typeEnum)
    => $"Values for {title}: {string.Join(", ", Enum.GetNames(typeEnum))}";

static string ExtensionFor(ReportFormat format) => format switch
{
    ReportFormat.Xlsx => "xlsx",
    ReportFormat.Html => "zip",
    _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
};
