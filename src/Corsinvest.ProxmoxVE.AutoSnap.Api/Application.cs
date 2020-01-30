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
using Corsinvest.ProxmoxVE.Api.Extension.Node;
using Corsinvest.ProxmoxVE.Api.Extension.VM;

namespace Corsinvest.ProxmoxVE.AutoSnap.Api
{
    /// <summary>
    /// Command autosnap.
    /// </summary>
    public class Application
    {
        private static readonly string PREFIX = "auto";

        private static readonly string TIMESTAMP_FORMAT = "yyMMddHHmmss";

        /// <summary>
        /// Application name
        /// </summary>
        public static readonly string NAME = "cv4pve-autosnap";

        /// <summary>
        /// Get label from description
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetLabelFromName(string name)
            => name.Substring(PREFIX.Length, name.Length - TIMESTAMP_FORMAT.Length - PREFIX.Length);

        /// <summary>
        /// Old application name
        /// </summary>
        private static readonly string OLD_NAME = "eve4pve-autosnap";

        private readonly PveClient _client;
        private readonly TextWriter _out;
        private readonly bool _dryRun;
        private readonly bool _debug;

        /// <summary>
        /// Constructor command
        /// </summary>
        /// <param name="client"></param>
        /// <param name="out"></param>
        /// <param name="dryRun"></param>
        /// <param name="debug"></param>
        public Application(PveClient client, TextWriter @out, bool dryRun, bool debug)
            => (_client, _out, _dryRun, _debug) = (client, @out, dryRun, debug);

        /// <summary>
        /// Event phase.
        /// </summary>
        public event EventHandler<PhaseEventArgs> PhaseEvent;

        private void CallPhaseEvent(string phase, VMInfo vm, string label, int keep, string snapName, bool state)
        {
            if (_debug) { _out.WriteLine($"Phase: {phase}"); }
            PhaseEvent?.Invoke(this, new PhaseEventArgs(phase,
                                                        vm,
                                                        label,
                                                        keep,
                                                        snapName,
                                                        state));
        }

        /// <summary>
        /// Status auto snapshot.
        /// </summary>
        /// <param name="vmIdsOrNames"></param>
        /// <returns></returns>
        public IEnumerable<Snapshot> Status(string vmIdsOrNames) => Status(vmIdsOrNames, null);

        /// <summary>
        /// Status auto snapshot.
        /// </summary>
        /// <param name="vmIdsOrNames"></param>
        /// <param name="label"></param>
        public IEnumerable<Snapshot> Status(string vmIdsOrNames, string label)
        {
            //select snapshot and filter
            var snapshots = FilterApp(_client.GetVMs(vmIdsOrNames)
                                             .Where(a => _client.GetNode(a.Node).IsOnline)
                                             .SelectMany(a => a.Snapshots));

            if (!string.IsNullOrWhiteSpace(label)) { snapshots = FilterLabel(snapshots, label); }

            return snapshots;
        }

        private static IEnumerable<Snapshot> FilterApp(IEnumerable<Snapshot> snapshots)
            => snapshots.Where(a => (a.Description == NAME || a.Description == OLD_NAME));

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
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool Snap(string vmIdsOrNames, string label, int keep, bool state, long timeout)
        {
            _out.WriteLine($@"ACTION Snap 
VMs:   {vmIdsOrNames}  
Label: {label} 
Keep:  {keep} 
State: {state}");

            CallPhaseEvent("snap-job-start", null, label, keep, null, state);

            var ret = true;

            foreach (var vm in _client.GetVMs(vmIdsOrNames)
                                      .Where(a => _client.GetNode(a.Node).IsOnline))
            {
                _out.WriteLine($"----- VM {vm.Id} -----");

                //exclude template
                if (vm.IsTemplate)
                {
                    _out.WriteLine("Skip VM is template");
                    continue;
                }

                //check agent enabled
                if (vm.Type == VMTypeEnum.Qemu && !((ConfigQemu)vm.Config).AgentEnabled)
                {
                    _out.WriteLine(((ConfigQemu)vm.Config).GetMessageEnablingAgent());
                }

                //create snapshot
                var snapName = GetPrefix(label) + DateTime.Now.ToString(TIMESTAMP_FORMAT);

                CallPhaseEvent("snap-create-pre", vm, label, keep, snapName, state);

                _out.WriteLine($"Create snapshot: {snapName}");

                var inError = true;
                if (!_dryRun)
                {
                    var result = vm.Snapshots.Create(snapName, NAME, state, timeout);
                    inError = result.LogInError(_out);

                    //check error in task
                    var task = _client.Nodes[vm.Node].Tasks[(result.Response.data as string)];
                    var data = task.Status.ReadTaskStatus().Response.data;
                    if (data.exitstatus != "OK")
                    {
                        _out.WriteLine($"Error in task: {data.exitstatus}");
                        inError = true;
                    }
                }

                if (inError)
                {
                    CallPhaseEvent("snap-create-abort", vm, label, keep, snapName, state);

                    ret = false;
                    continue;
                }

                CallPhaseEvent("snap-create-post", vm, label, keep, snapName, state);

                //remove old snapshot
                if (!SnapshotsRemove(vm, label, keep, timeout))
                {
                    ret = false;
                    continue;
                }
            }

            CallPhaseEvent("snap-job-end", null, label, keep, null, state);

            if (_debug) { _out.WriteLine($"Snap Exit: {ret}"); }

            return ret;
        }

        /// <summary>
        /// Clean autosnap.
        /// </summary>
        /// <param name="vmIdsOrNames"></param>
        /// <param name="label"></param>
        /// <param name="keep"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool Clean(string vmIdsOrNames, string label, int keep, long timeout)
        {
            _out.WriteLine($@"ACTION Clean 
VMs:   {vmIdsOrNames}  
Label: {label} 
Keep:  {keep}");

            var ret = true;
            CallPhaseEvent("clean-job-start", null, label, keep, null, false);

            foreach (var vm in _client.GetVMs(vmIdsOrNames)
                                      .Where(a => _client.GetNode(a.Node).IsOnline))
            {
                _out.WriteLine($"----- VM {vm.Id} -----");
                if (!SnapshotsRemove(vm, label, keep, timeout)) { ret = false; }
            }

            CallPhaseEvent("clean-job-end", null, label, keep, null, false);

            return ret;
        }

        private bool SnapshotsRemove(VMInfo vm, string label, int keep, long timeout)
        {
            foreach (var snapshot in FilterLabel(vm.Snapshots, label).Reverse().Skip(keep).Reverse())
            {
                CallPhaseEvent("snap-remove-pre", vm, label, keep, snapshot.Name, false);

                _out.WriteLine($"Remove snapshot: {snapshot.Name}");

                var inError = false;
                if (!_dryRun)
                {
                    inError = vm.Snapshots.Remove(snapshot, timeout).LogInError(_out);
                }

                if (inError)
                {
                    if (_debug) { _out.WriteLine($"Snap remove: problem in remove "); }

                    CallPhaseEvent("snap-remove-abort", vm, label, keep, snapshot.Name, false);
                    return false;
                }

                CallPhaseEvent("snap-remove-post", vm, label, keep, snapshot.Name, false);
            }

            return true;
        }
    }
}