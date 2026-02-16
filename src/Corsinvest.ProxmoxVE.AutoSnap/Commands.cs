/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.CommandLine;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Api.Console.Helpers;
using Corsinvest.ProxmoxVE.AutoSnap.Api;
using Microsoft.Extensions.Logging;

namespace Corsinvest.ProxmoxVE.AutoSnap;

/// <summary>
/// Console command.
/// </summary>
public class Commands
{
    private string? _scriptHook;
    private readonly ILoggerFactory _loggerFactory;
    private readonly bool _dryRun;
    private readonly bool _debug;
    private readonly TextWriter _out;

    public Commands(RootCommand command, ILoggerFactory loggerFactory)
    {
        _dryRun = command.DryRunIsActive();
        _debug = command.DebugIsActive();
        _out = Console.Out;

        _loggerFactory = loggerFactory;

        var optVmIds = command.VmIdsOrNamesOption();
        optVmIds.Required = true;

        var optTimeout = command.TimeoutOption();
        optTimeout.DefaultValueFactory = (_) => 30L;

        var optTimestampFormat = command.AddOption<string>("--timestamp-format", $"Specify different timestamp format");
        optTimestampFormat.DefaultValueFactory = (_) => Application.DefaultTimestampFormat;

        var optMaxPercentageStorage = command.AddOption<int>("--max-perc-storage", "Max percentage storage")
                                             .AddValidatorRange(1, 100);
        optMaxPercentageStorage.DefaultValueFactory = (_) => 95;

        Snap(command, optVmIds, optTimeout, optTimestampFormat, optMaxPercentageStorage);
        Clean(command, optVmIds, optTimeout, optTimestampFormat);
        Status(command, optVmIds, optTimestampFormat);
    }

    private async Task<Application> CreateAppAsync(RootCommand command)
    {
        var app = new Application(await command.ClientTryLoginAsync(_loggerFactory), _loggerFactory, _out, _dryRun);
        app.PhaseEvent += App_PhaseEvent;
        return app;
    }

    private Task App_PhaseEvent(PhaseEventArgs e)
    {
        if (_scriptHook == null || !File.Exists(_scriptHook)) { return Task.CompletedTask; }

        var (stdOut, exitCode) = ShellHelper.Execute(_scriptHook,
                                                     true,
                                                     new Dictionary<string, string>(e.Environments)
                                                     {
                                                        { "CV4PVE_AUTOSNAP_DEBUG", _debug ? "1" : "0" },
                                                        { "CV4PVE_AUTOSNAP_DRY_RUN", _dryRun ? "1" : "0" },
                                                     },
                                                     _out,
                                                     _dryRun,
                                                     _debug);

        if (exitCode != 0) { _out.WriteLine($"Script return code: {exitCode}"); }
        if (!string.IsNullOrWhiteSpace(stdOut)) { _out.Write(stdOut); }

        return Task.CompletedTask;
    }

    private static Option<string> OptionLabel(Command command)
        => command.AddOption<string>("--label", "Is usually 'hourly', 'daily', 'weekly', or 'monthly'");

    private static Option<int> OptionKeep(Command command, int min = 1)
    {
        var opt = command.AddOption<int>("--keep", "Specify the number which should will keep")
                         .AddValidatorRange(min, 100);

        opt.Required = true;
        return opt;
    }

    private void Status(RootCommand command, Option<string> optVmIds, Option<string> optTimestampFormat)
    {
        var cmd = command.AddCommand("status", "Get list of all auto snapshots");
        var optLabel = OptionLabel(cmd);
        var optOutput = cmd.TableOutputOption();

        cmd.SetAction(async (parseResult) =>
        {
            var app = await CreateAppAsync(command);
            var snapshots = await app.StatusAsync(parseResult.GetValue(optVmIds)!,
                                                  parseResult.GetValue(optLabel)!,
                                                  parseResult.GetValue(optTimestampFormat)!);
            if (snapshots.Any())
            {
                var rows = new List<object[]>();
                foreach (var (vm, items) in snapshots)
                {
                    rows.AddRange(items.Select(a => new object[] { vm.Node,
                                                                   vm.VmId,
                                                                   a.Date.ToString("yy/MM/dd HH:mm:ss"),
                                                                   a.Parent,
                                                                   a.Name,
                                                                   (a.Description + string.Empty).Replace("\n", string.Empty),
                                                                   a.VmStatus ? "X" : string.Empty }));
                }

                _out.Write(TableGenerator.To(["NODE", "VM", "TIME", "PARENT", "NAME", "DESCRIPTION", "VM STATUS"],
                                             rows,
                                             parseResult.GetValue(optOutput)));
            }
        });
    }

    private void Clean(RootCommand command,
                       Option<string> optVmIds,
                       Option<long> optTimeout,
                       Option<string> optTimestampFormat)
    {
        var cmd = command.AddCommand("clean", "Remove auto snapshots");
        var optLabel = OptionLabel(cmd);
        optLabel.Required = true;

        var optKeep = OptionKeep(cmd, 0);
        var optScript = cmd.ScriptFileOption();

        cmd.SetAction(async (parseResult) =>
        {
            _scriptHook = parseResult.GetValue(optScript);
            var app = await CreateAppAsync(command);
            return await app.CleanAsync(parseResult.GetValue(optVmIds)!,
                                        parseResult.GetValue(optLabel)!,
                                        parseResult.GetValue(optKeep),
                                        parseResult.GetValue(optTimeout) * 1000,
                                        parseResult.GetValue(optTimestampFormat)!) ? 0 : 1;
        });
    }

    private void Snap(RootCommand command,
                      Option<string> optVmIds,
                      Option<long> optTimeout,
                      Option<string> optTimestampFormat,
                      Option<int> optMaxPercentageStorage)
    {
        var cmd = command.AddCommand("snap", "Will snap one time");

        var optLabel = OptionLabel(cmd);
        optLabel.Required = true;

        var optKeep = OptionKeep(cmd);
        var optScript = cmd.ScriptFileOption();
        var optState = cmd.AddOption<bool>("--state", "Save the vmstate (Include RAM)");
        var optOnlyRunning = cmd.AddOption<bool>("--only-running", "Only VM/CT are running");

        cmd.SetAction(async (parseResult) =>
        {
            _scriptHook = parseResult.GetValue(optScript);
            var app = await CreateAppAsync(command);
            var snap = await app.SnapAsync(parseResult.GetValue(optVmIds)!,
                                           parseResult.GetValue(optLabel)!,
                                           parseResult.GetValue(optKeep),
                                           parseResult.GetValue(optState),
                                           parseResult.GetValue(optTimeout) * 1000,
                                           parseResult.GetValue(optTimestampFormat)!,
                                           parseResult.GetValue(optMaxPercentageStorage),
                                           parseResult.GetValue(optOnlyRunning));

            return snap.Status ? 0 : 1;
        });
    }
}