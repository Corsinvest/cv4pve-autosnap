/*
 * This file is part of the cv4pve-autosnap https://github.com/Corsinvest/cv4pve-autosnap,
 *
 * This source file is available under two different licenses:
 * - GNU General Public License version 3 (GPLv3)
 * - Corsinvest Enterprise License (CEL)
 * Full copyright and license information is available in
 * LICENSE.md which is distributed with this source code.
 *
 * Copyright (C) 2016 Corsinvest Srl	GPLv3 and CEL
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shell.Helpers;
using Corsinvest.ProxmoxVE.AutoSnap.Api;
using Corsinvest.ProxmoxVE.Api.Extension.VM;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Corsinvest.ProxmoxVE.Api.Extension.Helpers;

namespace Corsinvest.ProxmoxVE.AutoSnap
{
    /// <summary>
    /// Shell command.
    /// </summary>
    public class ShellCommands
    {
        private string _scriptHook;
        private readonly TextWriter _out;
        private readonly bool _dryRun;
        private readonly bool _debug;

        /// <summary>
        /// Shell command for cli.
        /// </summary>
        /// <param name="parent"></param>
        public ShellCommands(CommandLineApplication parent)
        {
            _out = parent.Out;
            _dryRun = parent.DryRunIsActive();
            _debug = parent.DebugIsActive();

            var optVmIds = parent.VmIdsOrNamesOption().DependOn(parent, CommandOptionExtension.HOST_OPTION_NAME);
            var optTimeout = parent.TimeoutOption();

            Snap(parent, optVmIds, optTimeout);
            Clean(parent, optVmIds, optTimeout);
            Status(parent, optVmIds);
        }

        private Application CreateApp(CommandLineApplication parent)
        {
            var app = new Application(parent.ClientTryLogin(), _out, _dryRun, _debug);
            app.PhaseEvent += App_PhaseEvent;
            return app;
        }

        private void App_PhaseEvent(object sender, PhaseEventArgs e)
        {
            if (!File.Exists(_scriptHook)) { return; }

            var ret = ShellHelper.Execute(_scriptHook,
                                          true,
                                          new Dictionary<string, string>(e.Environments)
                                          {
                                                {"CV4PVE_AUTOSNAP_DEBUG", _debug ? "1" :"0"},
                                                {"CV4PVE_AUTOSNAP_DRY_RUN", _dryRun ? "1" :"0"},
                                          },
                                          _out,
                                          _dryRun,
                                          _debug);

            if (ret.ExitCode != 0)
            {
                _out.WriteLine($"Script return code: {ret.ExitCode}");
            }

            if (!string.IsNullOrWhiteSpace(ret.StandardOutput))
            {
                _out.Write(ret.StandardOutput);
            }
        }

        private void Status(CommandLineApplication parent, CommandOption optVmIds)
        {
            parent.Command("status", cmd =>
            {
                cmd.Description = "Get list of all auto snapshots";
                cmd.AddFullNameLogo();

                var optLabel = cmd.LabelOption();
                var optOutput = cmd.OptionEnum<OutputType>("--output|-o", "Type output (default: unicode)");

                cmd.OnExecute(() =>
                {
                    var snapshots = CreateApp(parent).Status(optVmIds.Value(), optLabel.Value());
                    var outputType = optOutput.GetEnumValue<OutputType>();

                    switch (outputType)
                    {
                        case OutputType.Text:
                        case OutputType.Html:
                        case OutputType.Markdown:
                        case OutputType.Unicode:
                        case OutputType.UnicodeAlt:

                            var tableOutputType = TableOutputType.Unicode;
                            switch (outputType)
                            {
                                case OutputType.Html: tableOutputType = TableOutputType.Html; break;
                                case OutputType.Markdown: tableOutputType = TableOutputType.Markdown; break;
                                case OutputType.Text: tableOutputType = TableOutputType.Text; break;
                                case OutputType.Unicode: tableOutputType = TableOutputType.Unicode; break;
                                case OutputType.UnicodeAlt: tableOutputType = TableOutputType.UnicodeAlt; break;
                                default: tableOutputType = TableOutputType.Unicode; break;
                            }

                            parent.Out.Write(snapshots.Info(true, tableOutputType));
                            break;

                        case OutputType.Json:
                            parent.Out.Write(JsonConvert.SerializeObject((snapshots.Select(a => a.GetRowInfo(true)))));
                            break;

                        case OutputType.JsonPretty:
                            parent.Out.Write(JsonConvert.SerializeObject((snapshots.Select(a => a.GetRowInfo(true))),
                                                                         Formatting.Indented));
                            break;

                        default:
                            parent.Out.Write(snapshots.Info(true, TableOutputType.Unicode));
                            break;
                    }
                });
            });
        }

        private void Clean(CommandLineApplication parent,
                           CommandOption optVmIds,
                           CommandOption<long> optTimeout)
        {
            parent.Command("clean", cmd =>
            {
                cmd.Description = "Remove auto snapshots";
                cmd.AddFullNameLogo();

                var optLabel = cmd.LabelOption().IsRequired();

                var optKeep = cmd.KeepOption().IsRequired();
                optKeep.Accepts().Range(0, 999, "--keep Rang not valid!");

                var optScriptHook = cmd.ScriptHookOption();

                cmd.OnExecute(() =>
                {
                    _scriptHook = optScriptHook.Value();
                    return CreateApp(parent).Clean(optVmIds.Value(),
                                                   optLabel.Value(),
                                                   optKeep.ParsedValue,
                                                   optTimeout.HasValue() ?
                                                        optTimeout.ParsedValue * 1000 :
                                                        ResultExtension.DEFAULT_TIMEOUT) ? 0 : 1;
                });
            });
        }

        private void Snap(CommandLineApplication parent,
                          CommandOption optVmIds,
                          CommandOption<long> optTimeout)
        {
            parent.Command("snap", cmd =>
            {
                cmd.Description = "Will snap one time";
                cmd.AddFullNameLogo();

                var optLabel = cmd.LabelOption().IsRequired();
                var optKeep = cmd.KeepOption().IsRequired();
                var optState = cmd.VmStateOption();
                var optScriptHook = cmd.ScriptHookOption();

                cmd.OnExecute(() =>
                {
                    _scriptHook = optScriptHook.Value();
                    return CreateApp(parent).Snap(optVmIds.Value(),
                                                  optLabel.Value(),
                                                  optKeep.ParsedValue,
                                                  optState.HasValue(),
                                                  optTimeout.HasValue() ?
                                                    optTimeout.ParsedValue * 1000 :
                                                    ResultExtension.DEFAULT_TIMEOUT) ? 0 : 1;
                });
            });
        }
    }
}