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
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Extension.VM;
using Corsinvest.ProxmoxVE.Api.Shell.Helpers;
using McMaster.Extensions.CommandLineUtils;

namespace Corsinvest.ProxmoxVE.AutoSnap
{
    /// <summary>
    /// Shell command.
    /// </summary>
    public class ShellCommands
    {
        private string _scriptHook;

        /// <summary>
        /// Shell command for cli.
        /// </summary>
        /// <param name="parent"></param>
        public ShellCommands(CommandLineApplication parent)
        {
            var optVmIds = parent.VmIdsOrNamesOption().DependOn(parent, CommandOptionExtension.HOST_OPTION_NAME);
            var optTimeout = parent.TimeoutOption();

            Snap(parent, optVmIds, optTimeout);
            Clean(parent, optVmIds, optTimeout);
            Status(parent, optVmIds);
        }

        private Commands CreateApp(CommandLineApplication parent)
        {
            var app = new Commands(parent.ClientTryLogin(),
                                   parent.Out,
                                   parent.DryRunIsActive(),
                                   parent.DebugIsActive());

            app.PhaseEvent += App_PhaseEvent;
            return app;
        }

        private void App_PhaseEvent(object sender,
                                    (string phase,
                                     VMInfo vm,
                                     string label,
                                     int keep,
                                     string snapName,
                                     bool state,
                                     TextWriter stdOut,
                                     bool dryRun,
                                     bool debug) e)
        {
            if (!File.Exists(_scriptHook)) { return; }

            var ret = ShellHelper.Execute(_scriptHook,
                                          true,
                                          new Dictionary<string, string>
                                          {
                                              {"CV4PVE_AUTOSNAP_PHASE", e.phase},
                                              {"CV4PVE_AUTOSNAP_VMID", e.vm?.Id + ""},
                                              {"CV4PVE_AUTOSNAP_VMTYPE", e.vm?.Type + ""},
                                              {"CV4PVE_AUTOSNAP_LABEL", e.label},
                                              {"CV4PVE_AUTOSNAP_KEEP", e.keep + ""},
                                              {"CV4PVE_AUTOSNAP_SNAP_NAME", e.snapName},
                                              {"CV4PVE_AUTOSNAP_VMSTATE", e.state ? "1" :"0"},
                                              {"CV4PVE_AUTOSNAP_DEBUG", e.debug ? "1" :"0"},
                                              {"CV4PVE_AUTOSNAP_DRY_RUN", e.dryRun ? "1" :"0"},
                                          },
                                          e.stdOut,
                                          e.dryRun,
                                          e.debug);

            if (ret.ExitCode != 0)
            {
                e.stdOut.WriteLine($"Script return code: {ret.ExitCode}");
            }

            if (!string.IsNullOrWhiteSpace(ret.StandardOutput))
            {
                e.stdOut.Write(ret.StandardOutput);
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

                cmd.OnExecute(() => CreateApp(parent).Status(optVmIds.Value(),
                                                             optLabel.Value(),
                                                             optOutput.GetEnumValue<OutputType>()));
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