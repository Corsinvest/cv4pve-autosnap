using System;
using System.Collections.Generic;
using System.IO;
using Corsinvest.ProxmoxVE.Api.Extension.Shell.Utils;
using Corsinvest.ProxmoxVE.Api.Extension.VM;
using Corsinvest.ProxmoxVE.AutoSnap.Api;
using McMaster.Extensions.CommandLineUtils;

namespace Corsinvest.ProxmoxVE.Api.Extension.App.AutoSnap
{
    /// <summary>
    /// Shell command.
    /// </summary>
    public class ShellCommand
    {
        private string _scriptHook;
        private readonly bool _dryRun;
        private readonly bool _debug;

        /// <summary>
        /// Shell command for cli.
        /// </summary>
        /// <param name="app"></param>
        public ShellCommand(CommandLineApplication app)
        {
            _dryRun = app.DryRunIsActive();
            _debug = app.DebugIsActive();

            app.AddLoginOptions();
            var optVmIds = app.VmIdsOrNamesOption();

            CmdSnap(app, optVmIds);
            CmdClean(app, optVmIds);
            CmdStatus(app, optVmIds);

            app.OnExecute(() =>
            {
                app.ShowHint();
                return 1;
            });
        }

        /// <summary>
        /// Main for cli.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static int MainForCli(string[] args)
        {
            var app = ShellHelper.CreateConsoleApp(Command.APPLICATION_NAME, "Automatic snapshot with retention");
            new ShellCommand(app);
            return app.ExecuteConsoleApp(Console.Out, args);
        }

        private Command CreateApp(CommandLineApplication autosnapCmd)
        {
            var app = new Command(autosnapCmd.ClientTryLogin(), Console.Out, _dryRun, _debug);
            app.PhaseEvent += App_PhaseEvent;
            return app;
        }

        private void App_PhaseEvent(object sender,
                                    (string phase, VMInfo vm, string label, int keep, string snapName, bool state, TextWriter stdOut, bool dryRun, bool debug) e)
        {
            if (!File.Exists(_scriptHook)) { return; }

            ShellHelper.Execute(_scriptHook,
                                e.debug,
                                new Dictionary<string, string>()
                                {
                                    {"CV4PVE_AUTOSNAP_PHASE", e.phase},
                                    {"CV4PVE_AUTOSNAP_VMID", e.vm?.Id + ""},
                                    {"CV4PVE_AUTOSNAP_VMTYPE", e.vm?.Type + ""},
                                    {"CV4PVE_AUTOSNAP_LABEL", e.label},
                                    {"CV4PVE_AUTOSNAP_KEEP", e.keep + ""},
                                    {"CV4PVE_AUTOSNAP_SNAP_NAME", e.snapName},
                                    {"CV4PVE_AUTOSNAP_VMSTATE", e.state ? "1" :"0"},
                                },
                                e.stdOut,
                                e.dryRun,
                                e.debug);
        }

        private void CmdStatus(CommandLineApplication autosnapCmd, CommandOption optVmIds)
        {
            autosnapCmd.Command("status", cmd =>
            {
                cmd.Description = "Get list of all auto snapshots";
                cmd.AddFullNameLogo();

                var optLabel = cmd.LabelOption(false);

                cmd.OnExecute(() => CreateApp(autosnapCmd).Status(optVmIds.Value(), optLabel.Value()));
            });
        }

        private void CmdClean(CommandLineApplication autosnapCmd, CommandOption optVmIds)
        {
            autosnapCmd.Command("clean", cmd =>
            {
                cmd.Description = "Remove auto snapshots";
                cmd.AddFullNameLogo();

                var optLabel = cmd.LabelOption(true);

                var optKeep = cmd.KeepOption();
                optKeep .Accepts().Range(0, 999, "--keep Rang not valid!");

                var optScriptHook = cmd.ScriptHookOption();

                cmd.OnExecute(() =>
                {
                    _scriptHook = optScriptHook.Value();
                    return CreateApp(autosnapCmd).Clean(optVmIds.Value(),
                                                        optLabel.Value(),
                                                        optKeep.ParsedValue) ? 0 : 1;
                });
            });
        }

        private void CmdSnap(CommandLineApplication autosnapCmd, CommandOption optVmIds)
        {
            autosnapCmd.Command("snap", cmd =>
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
                    return CreateApp(autosnapCmd).Snap(optVmIds.Value(),
                                                       optLabel.Value(),
                                                       optKeep.ParsedValue,
                                                       optState.HasValue()) ? 0 : 1;
                });
            });
        }
    }
}