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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Extension.VM;
using Newtonsoft.Json;

namespace Corsinvest.ProxmoxVE.AutoSnap
{
    /// <summary>
    /// Command autosnap.
    /// </summary>
    public class Commands
    {
        private static readonly string PREFIX = "auto";

        private static readonly string TIMESTAMP_FORMAT = "yyMMddHHmmss";

        /// <summary>
        /// Application name
        /// </summary>
        public static readonly string APPLICATION_NAME = "cv4pve-autosnap";

        /// <summary>
        /// Old application name
        /// </summary>
        private static readonly string OLD_APPLICATION_NAME = "eve4pve-autosnap";

        private readonly PveClient _client;
        private readonly TextWriter _stdOut;
        private readonly bool _dryRun;
        private readonly bool _debug;

        /// <summary>
        /// Constructor command
        /// </summary>
        /// <param name="client"></param>
        /// <param name="stdOut"></param>
        /// <param name="dryRun"></param>
        /// <param name="debug"></param>
        public Commands(PveClient client, TextWriter stdOut, bool dryRun, bool debug)
            => (_client, _stdOut, _dryRun, _debug) = (client, stdOut, dryRun, debug);

        /// <summary>
        /// Event phase.
        /// </summary>
        public event EventHandler<(string Phase, VMInfo VM, string Label, int Keep, string SnapName, bool State, TextWriter StdOut, bool DryRun, bool Debug)> PhaseEvent;

        private void CallPhaseEvent(string phase, VMInfo vm, string label, int keep, string snapName, bool state)
        {
            if (_debug) { _stdOut.WriteLine($"Phase: {phase}"); }
            PhaseEvent?.Invoke(this, (phase, vm, label, keep, snapName, state, _stdOut, _dryRun, _debug));
        }

        /// <summary>
        /// Status auto snapshot.
        /// </summary>
        /// <param name="vmIdsOrNames"></param>
        /// <param name="label"></param>
        public void Status(string vmIdsOrNames, string label = null, OutputType outputType = OutputType.Text)
        {
            //select snapshot and filter
            var snapshots = FilterApp(_client.GetVMs(vmIdsOrNames).SelectMany(a => a.Snapshots));

            if (!string.IsNullOrWhiteSpace(label)) { snapshots = FilterLabel(snapshots, label); }

            switch (outputType)
            {
                case OutputType.Text:
                    _stdOut.Write(snapshots.Info(true));
                    break;

                case OutputType.Json:
                    _stdOut.Write(JsonConvert.SerializeObject((snapshots.Select(a => a.GetRowInfo(true)))));
                    break;

                case OutputType.JsonPretty:
                    _stdOut.Write(JsonConvert.SerializeObject((snapshots.Select(a => a.GetRowInfo(true))), Formatting.Indented));
                    break;

                default:
                    _stdOut.Write(snapshots.Info(true));
                    break;
            }
        }

        private static IEnumerable<Snapshot> FilterApp(IEnumerable<Snapshot> snapshots)
            => snapshots.Where(a => (a.Description == APPLICATION_NAME || a.Description == OLD_APPLICATION_NAME));

        private static string GetPrefix(string label) => PREFIX + label;

        private static IEnumerable<Snapshot> FilterLabel(IEnumerable<Snapshot> snapshots, string label)
            => FilterApp(snapshots.Where(a => (a.Name.Length - TIMESTAMP_FORMAT.Length) > 0 &&
                                               a.Name.Substring(0, a.Name.Length - TIMESTAMP_FORMAT.Length) == GetPrefix(label)));

        /// <summary>
        /// Execute a autosnap.
        /// </summary>
        /// <param name="vmIdsOrNames"></param>
        /// <param name="label"></param>
        /// <param name="keep"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool Snap(string vmIdsOrNames, string label, int keep, bool state)
        {
            _stdOut.WriteLine($@"ACTION Snap 
VMs:   {vmIdsOrNames}  
Label: {label} 
Keep:  {keep} 
State: {state}");

            CallPhaseEvent("snap-job-start", null, label, keep, null, state);

            var ret = true;

            foreach (var vm in _client.GetVMs(vmIdsOrNames))
            {
                _stdOut.WriteLine($"----- VM {vm.Id} -----");

                //exclude template
                if (vm.IsTemplate)
                {
                    _stdOut.WriteLine("Skip VM is template");
                    continue;
                }

                //check agent enabled
                if (vm.Type == VMTypeEnum.Qemu && !((ConfigQemu)vm.Config).AgentEnabled)
                {
                    _stdOut.WriteLine(((ConfigQemu)vm.Config).GetMessageEnablingAgent());
                }

                //create snapshot
                var snapName = GetPrefix(label) + DateTime.Now.ToString(TIMESTAMP_FORMAT);

                CallPhaseEvent("snap-create-pre", vm, label, keep, snapName, state);

                _stdOut.WriteLine($"Create snapshot: {snapName}");

                var inError = true;
                if (!_dryRun)
                {
                    var oldWaitTimeout = ResultExtension.WaitTimeout;
                    ResultExtension.WaitTimeout = 30000;

                    var result = vm.Snapshots.Create(snapName, APPLICATION_NAME, state, true);
                    inError = result.LogInError(_stdOut);

                    //check error in task
                    var task = _client.Nodes[vm.Node].Tasks[(result.Response.data as string)];
                    var data = task.Status.ReadTaskStatus().Response.data;
                    if (data.exitstatus != "OK")
                    {
                        _stdOut.WriteLine($"Error in task: {data.exitstatus}");
                        inError = true;
                    }

                    ResultExtension.WaitTimeout = oldWaitTimeout;
                }

                if (inError)
                {
                    CallPhaseEvent("snap-create-abort", vm, label, keep, snapName, state);

                    ret = false;
                    break;
                }

                CallPhaseEvent("snap-create-post", vm, label, keep, snapName, state);

                //remove old snapshot
                if (!SnapshotsRemove(vm, label, keep))
                {
                    ret = false;
                    break;
                }
            }

            CallPhaseEvent("snap-job-end", null, label, keep, null, state);

            if (_debug) { _stdOut.WriteLine($"Snap Exit: {ret}"); }

            return ret;
        }

        /// <summary>
        /// Clean autosnap.
        /// </summary>
        /// <param name="vmIdsOrNames"></param>
        /// <param name="label"></param>
        /// <param name="keep"></param>
        /// <returns></returns>
        public bool Clean(string vmIdsOrNames, string label, int keep)
        {
            _stdOut.WriteLine($@"ACTION Clean 
VMs:   {vmIdsOrNames}  
Label: {label} 
Keep:  {keep}");

            var ret = true;
            CallPhaseEvent("clean-job-start", null, label, keep, null, false);

            foreach (var vm in _client.GetVMs(vmIdsOrNames))
            {
                _stdOut.WriteLine($"----- VM {vm.Id} -----");
                if (!SnapshotsRemove(vm, label, keep)) { ret = false; }
            }

            CallPhaseEvent("clean-job-end", null, label, keep, null, false);

            return ret;
        }

        private bool SnapshotsRemove(VMInfo vm, string label, int keep)
        {
            foreach (var snapshot in FilterLabel(vm.Snapshots, label).Reverse().Skip(keep).Reverse())
            {
                CallPhaseEvent("snap-remove-pre", vm, label, keep, snapshot.Name, false);

                _stdOut.WriteLine($"Remove snapshot: {snapshot.Name}");

                var inError = false;
                if (!_dryRun)
                {
                    var oldWaitTimeout = ResultExtension.WaitTimeout;
                    ResultExtension.WaitTimeout = 30000;

                    inError = vm.Snapshots.Remove(snapshot, true).LogInError(_stdOut);

                    ResultExtension.WaitTimeout = oldWaitTimeout;
                }

                if (inError)
                {
                    if (_debug) { _stdOut.WriteLine($"Snap remove: problem in remove "); }

                    CallPhaseEvent("snap-remove-abort", vm, label, keep, snapshot.Name, false);
                    return false;
                }

                CallPhaseEvent("snap-remove-post", vm, label, keep, snapshot.Name, false);
            }

            return true;
        }
    }
}