/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: GPL-3.0-only
 */

using Corsinvest.ProxmoxVE.Api.Console.Helpers;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Common;
using Corsinvest.ProxmoxVE.Report;
using Microsoft.Extensions.Logging;
using System.Text.Json;

const string settingsFileName = "settings.json";

var app = ConsoleHelper.CreateApp("cv4pve-report", "Report for Proxmox VE");

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

var cmdExport = app.AddCommand("export", "Generate Excel report");
var optExportFast = cmdExport.AddOption<bool>("--fast", "Use fast profile (structure only, no heavy data)");
var optExportFull = cmdExport.AddOption<bool>("--full", "Use full profile (everything enabled, RRD on week timeframe)");
var optOutput = cmdExport.AddOption<string>("--output|-o", "Output file path (default: Report_YYYYMMDD_HHmmss.xlsx in current directory)");
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
    var progress = new Progress<ReportProgress>(p =>
    {
        if (Console.IsOutputRedirected)
        {
            Console.Out.WriteLine(p);
        }
        else
        {
            Console.Write($"\r{p,-60}");
        }
    });

    var output = action.GetValue(optOutput);
    var outputPath = !string.IsNullOrWhiteSpace(output)
                         ? output
                         : Path.Combine(".", $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

    await using var stream = await engine.GenerateAsync(progress);
    await using var file = File.Create(outputPath);
    await stream.CopyToAsync(file);

    if (!Console.IsOutputRedirected) { Console.WriteLine(); }
    Console.Out.WriteLine($"Report generated: {outputPath}");
});

return await app.ExecuteAppAsync(args, logger);

static string PrintEnum(string title, Type typeEnum)
    => $"Values for {title}: {string.Join(", ", Enum.GetNames(typeEnum))}";
