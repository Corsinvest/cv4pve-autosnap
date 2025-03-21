/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: 2019 Copyright Corsinvest Srl
 */

using System.CommandLine;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Api.Shell.Helpers;
using Corsinvest.ProxmoxVE.AutoSnap.Api;
using Microsoft.Extensions.Logging;

namespace Corsinvest.ProxmoxVE.AutoSnap;

/// <summary>
/// Shell command.
/// </summary>
public class Commands
{
    private string _scriptHook;
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
        optVmIds.IsRequired = true;

        var optTimeout = command.TimeoutOption();
        optTimeout.SetDefaultValue(30L);

        var optTimestampFormat = command.AddOption<string>("--timestamp-format", $"Specify different timestamp format");
        optTimestampFormat.SetDefaultValue(Application.DefaultTimestampFormat);

        var optMaxPercentageStorage = command.AddOption<int>("--max-perc-storage", "Max percentage storage")
                                             .AddValidatorRange(1, 100);
        optMaxPercentageStorage.SetDefaultValue(95);

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

    private void App_PhaseEvent(object sender, PhaseEventArgs e)
    {
        if (!File.Exists(_scriptHook)) { return; }

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
    }

    private static Option<string> OptionLabel(Command command)
        => command.AddOption<string>("--label", "Is usually 'hourly', 'daily', 'weekly', or 'monthly'");

    private static Option<int> OptionKeep(Command command, int min = 1)
    {
        var opt = command.AddOption<int>("--keep", "Specify the number which should will keep")
                         .AddValidatorRange(min, 100);

        opt.IsRequired = true;
        return opt;
    }

    private void Status(RootCommand command, Option<string> optVmIds, Option<string> optTimestampFormat)
    {
        var cmd = command.AddCommand("status", "Get list of all auto snapshots");
        var optLabel = OptionLabel(cmd);
        var optOutput = cmd.TableOutputOption();

        cmd.SetHandler(async (vmIds, label, timestampFormat, output) =>
        {
            var app = await CreateAppAsync(command);
            var snapshots = await app.StatusAsync(vmIds, label, timestampFormat);
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
                                                                   (a.Description + "").Replace("\n", ""),
                                                                   a.VmStatus ? "X" : "" }));
                }

                _out.Write(TableGenerator.To(["NODE", "VM", "TIME", "PARENT", "NAME", "DESCRIPTION", "VM STATUS"],
                                             rows,
                                             output));
            }
        }, optVmIds, optLabel, optTimestampFormat, optOutput);
    }

    private void Clean(RootCommand command,
                       Option<string> optVmIds,
                       Option<long> optTimeout,
                       Option<string> optTimestampFormat)
    {
        var cmd = command.AddCommand("clean", "Remove auto snapshots");
        var optLabel = OptionLabel(cmd);
        optLabel.IsRequired = true;

        var optKeep = OptionKeep(cmd, 0);
        var optScript = cmd.ScriptFileOption();

        cmd.SetHandler(async (ctx) =>
        {
            _scriptHook = ctx.ParseResult.GetValueForOption(optScript);
            var app = await CreateAppAsync(command);
            ctx.ExitCode = await app.CleanAsync(ctx.ParseResult.GetValueForOption(optVmIds),
                                                ctx.ParseResult.GetValueForOption(optLabel),
                                                ctx.ParseResult.GetValueForOption(optKeep),
                                                ctx.ParseResult.GetValueForOption(optTimeout) * 1000,
                                                ctx.ParseResult.GetValueForOption(optTimestampFormat)) ? 0 : 1;
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
        optLabel.IsRequired = true;

        var optKeep = OptionKeep(cmd);
        var optScript = cmd.ScriptFileOption();
        var optState = cmd.AddOption<bool>("--state", "Save the vmstate (Include RAM)");
        var optOnlyRunning = cmd.AddOption<bool>("--only-running", "Only VM/CT are running");

        cmd.SetHandler(async (ctx) =>
        {
            _scriptHook = ctx.ParseResult.GetValueForOption(optScript);
            var app = await CreateAppAsync(command);
            var snap = await app.SnapAsync(ctx.ParseResult.GetValueForOption(optVmIds),
                                      ctx.ParseResult.GetValueForOption(optLabel),
                                      ctx.ParseResult.GetValueForOption(optKeep),
                                      ctx.ParseResult.GetValueForOption(optState),
                                      ctx.ParseResult.GetValueForOption(optTimeout) * 1000,
                                      ctx.ParseResult.GetValueForOption(optTimestampFormat),
                                      ctx.ParseResult.GetValueForOption(optMaxPercentageStorage),
                                      ctx.ParseResult.GetValueForOption(optOnlyRunning));

            ctx.ExitCode = snap.Status ? 0 : 1;
        });
    }
}