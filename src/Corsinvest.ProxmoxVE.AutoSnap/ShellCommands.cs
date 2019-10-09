/*
 * This file is part of the cv4pve-autosnap https://github.com/Corsinvest/cv4pve-autosnap,
 * Copyright (C) 2016 Corsinvest Srl
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using Corsinvest.ProxmoxVE.Api.Extension.Helpers.Shell;
using Corsinvest.ProxmoxVE.Api.Extension.VM;
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
            var optVmIds = parent.VmIdsOrNamesOption();

            Snap(parent, optVmIds);
            Clean(parent, optVmIds);
            Status(parent, optVmIds);
        }

        private Commands CreateApp(CommandLineApplication parent)
        {
            var app = new Commands(parent.ClientTryLogin(),
                                   Console.Out,
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
                                          new Dictionary<string, string>()
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
                e.stdOut.WriteLine($"Script resturn code: {ret.ExitCode}");
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

                var optLabel = cmd.LabelOption(false);
                var optOutput = cmd.OptionEnum<OutputType>("--output|-o", "Type output (default: text)");

                cmd.OnExecute(() => CreateApp(parent).Status(optVmIds.Value(),
                                                             optLabel.Value(),
                                                             optOutput.GetEnumValue<OutputType>()));
            });
        }

        private void Clean(CommandLineApplication parent, CommandOption optVmIds)
        {
            parent.Command("clean", cmd =>
            {
                cmd.Description = "Remove auto snapshots";
                cmd.AddFullNameLogo();

                var optLabel = cmd.LabelOption(true);

                var optKeep = cmd.KeepOption();
                optKeep.Accepts().Range(0, 999, "--keep Rang not valid!");

                var optScriptHook = cmd.ScriptHookOption();

                cmd.OnExecute(() =>
                {
                    _scriptHook = optScriptHook.Value();
                    return CreateApp(parent).Clean(optVmIds.Value(),
                                                   optLabel.Value(),
                                                   optKeep.ParsedValue) ? 0 : 1;
                });
            });
        }

        private void Snap(CommandLineApplication parent, CommandOption optVmIds)
        {
            parent.Command("snap", cmd =>
            {
                cmd.Description = "Will snap one time";
                cmd.AddFullNameLogo();

                var optLabel = cmd.LabelOption(true);
                var optKeep = cmd.KeepOption();
                var optState = cmd.VmStateOption();
                var optScriptHook = cmd.ScriptHookOption();

                cmd.OnExecute(() =>
                {
                    _scriptHook = optScriptHook.Value();
                    return CreateApp(parent).Snap(optVmIds.Value(),
                                                  optLabel.Value(),
                                                  optKeep.ParsedValue,
                                                  optState.HasValue()) ? 0 : 1;
                });
            });
        }
    }
}