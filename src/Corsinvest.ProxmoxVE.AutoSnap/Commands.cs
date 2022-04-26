/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: 2019 Copyright Corsinvest Srl
 */

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Api.Shell.Helpers;
using Corsinvest.ProxmoxVE.AutoSnap.Api;
using Microsoft.Extensions.Logging;

namespace Corsinvest.ProxmoxVE.AutoSnap
{
    /// <summary>
    /// Shell command.
    /// </summary>
    public class Commands
    {
        private string _scriptHook;
        private readonly ILoggerFactory _loggerFactory;
        private readonly bool _dryRun;
        private readonly bool _debug;

        public Commands(RootCommand command)
        {
            _dryRun = command.DryRunIsActive();
            _debug = command.DebugIsActive();

            _loggerFactory = ConsoleHelper.CreateLoggerFactory<Program>(command.GetLogLevelFromDebug());

            var optVmIds = command.VmIdsOrNamesOption();
            optVmIds.IsRequired = true;

            var optTimeout = command.TimeoutOption();
            optTimeout.SetDefaultValue(30);

            var optTimestampFormat = command.AddOption("--timestamp-format", $"Specify different timestamp format");
            optTimestampFormat.SetDefaultValue(Application.DefaultTimestampFormat);

            var optMaxPercentageStorage = command.AddOption<int>("--max-perc-storage", "Max percentage storage")
                                                 .AddValidatorRange(1, 100);
            optMaxPercentageStorage.SetDefaultValue(95);

            Snap(command, optVmIds, optTimeout, optTimestampFormat, optMaxPercentageStorage);
            Clean(command, optVmIds, optTimeout, optTimestampFormat);
            Status(command, optVmIds, optTimestampFormat);
        }

        private async Task<Application> CreateApp(RootCommand command)
        {
            var app = new Application(await command.ClientTryLogin(_loggerFactory), _loggerFactory, Console.Out, _dryRun);
            app.PhaseEvent += App_PhaseEvent;
            return app;
        }

        private void App_PhaseEvent(object sender, PhaseEventArgs e)
        {
            if (!File.Exists(_scriptHook)) { return; }

            var (StandardOutput, ExitCode) = ShellHelper.Execute(_scriptHook,
                                                                 true,
                                                                 new Dictionary<string, string>(e.Environments)
                                                                 {
                                                                    { "CV4PVE_AUTOSNAP_DEBUG", _debug ? "1" : "0" },
                                                                    { "CV4PVE_AUTOSNAP_DRY_RUN", _dryRun ? "1" : "0" },
                                                                 },
                                                                 Console.Out,
                                                                 _dryRun,
                                                                 _debug);

            if (ExitCode != 0) { Console.Out.WriteLine($"Script return code: {ExitCode}"); }
            if (!string.IsNullOrWhiteSpace(StandardOutput)) { Console.Out.Write(StandardOutput); }
        }

        private static Option OptionLabel(Command command) => command.AddOption("--label", "Is usually 'hourly', 'daily', 'weekly', or 'monthly'");
        private static Option<int> OptionKeep(Command command, int min = 1)
        {
            var opt = command.AddOption<int>("--keep", "Specify the number which should will keep")
                                 .AddValidatorRange(min, 100);
            opt.IsRequired = true;
            return opt;
        }

        private static Option OptionScript(Command command)
            => command.AddOption("--script", "Use specified hook script").AddValidatorExistFile();


        private void Status(RootCommand command, Option optVmIds, Option optTimestampFormat)
        {
            var cmd = command.AddCommand("status", "Get list of all auto snapshots");
            var optLabel = OptionLabel(cmd);
            var optOutput = cmd.TableOutputOption();

            cmd.SetHandler(async () =>
            {
                var app = await CreateApp(command);
                var snapshots = await app.Status(optVmIds.GetValue(), optLabel.GetValue(), optTimestampFormat.GetValue());

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
                                                                       a.Description,
                                                                       a.VmStatus ? "X" : "" }));
                    }

                    Console.Out.Write(TableGenerator.To(new[] { "NODE", "VM", "TIME", "PARENT", "NAME", "DESCRIPTION", "VM STATUS" },
                                                        rows,
                                                        optOutput.GetValue()));
                }
            });
        }

        private void Clean(RootCommand command,
                           Option optVmIds,
                           Option<long> optTimeout,
                           Option optTimestampFormat)
        {
            var cmd = command.AddCommand("clean", "Remove auto snapshots");
            var optLabel = OptionLabel(cmd);
            optLabel.IsRequired = true;

            var optKeep = OptionKeep(cmd, 0);
            var optScript = OptionScript(cmd);

            cmd.SetHandler(async (InvocationContext ctx) =>
            {
                _scriptHook = optScript.GetValue();
                var app = await CreateApp(command);

                ctx.ExitCode = await app.Clean(optVmIds.GetValue(),
                                               optLabel.GetValue(),
                                               optKeep.GetValue(),
                                               optTimeout.GetValue() * 1000,
                                               optTimestampFormat.GetValue()) ? 0 : 1;
            });
        }

        private void Snap(RootCommand command,
                          Option optVmIds,
                          Option<long> optTimeout,
                          Option optTimestampFormat,
                          Option<int> optMaxPercentageStorage)
        {
            var cmd = command.AddCommand("snap", "Will snap one time");

            var optLabel = OptionLabel(cmd);
            optLabel.IsRequired = true;

            var optKeep = OptionKeep(cmd);
            var optScript = OptionScript(cmd);

            var optState = cmd.AddOption<bool>("--state", "Save the vmstate");
            //var optRunning = cmd.AddOption<bool>("--only-runs", "Only VM/CT are running");

            cmd.SetHandler(async (InvocationContext ctx) =>
            {
                _scriptHook = optScript.GetValue();
                var app = await CreateApp(command);
                var snap = await app.Snap(optVmIds.GetValue(),
                                          optLabel.GetValue(),
                                          optKeep.GetValue(),
                                          optState.GetValue(),
                                          optTimeout.GetValue() * 1000,
                                          optTimestampFormat.GetValue(),
                                          optMaxPercentageStorage.GetValue());

                ctx.ExitCode = snap.Status ? 0 : 1;
            });
        }
    }
}