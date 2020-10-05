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
using System.Diagnostics;
using System.IO;
using System.Linq;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
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
        public static string GetLabelFromName(string name) => name[PREFIX.Length..^TIMESTAMP_FORMAT.Length];

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

        /// <summary>
        /// Phases
        /// </summary>
        /// <value></value>
        public static Dictionary<string, HookPhase> Phases = new Dictionary<string, HookPhase>
        {
            { "clean-job-start" , HookPhase.CleanJobStart },
            { "clean-job-end" , HookPhase.CleanJobEnd },
            { "snap-job-start" , HookPhase.SnapJobStart },
            { "snap-job-end" , HookPhase.SnapJobEnd },
            { "snap-create-pre" , HookPhase.SnapCreatePre },
            { "snap-create-post" , HookPhase.SnapCreatePost },
            { "snap-create-abort" , HookPhase.SnapCreateAbort },
            { "snap-remove-pre" , HookPhase.SnapRemovePre },
            { "snap-remove-post" , HookPhase.SnapRemovePost },
            { "snap-remove-abort" , HookPhase.SnapRemoveAbort }
        };

        /// <summary>
        /// Phase string to enum
        /// </summary>
        /// <param name="phase"></param>
        /// <returns></returns>
        public static HookPhase PhaseStrToEnum(string phase) => Phases[phase];

        /// <summary>
        /// Phase enum to string
        /// </summary>
        /// <param name="phase"></param>
        /// <returns></returns>
        public static string PhaseEnumToStr(HookPhase phase) => Phases.SingleOrDefault(a => a.Value == phase).Key;

        private void CallPhaseEvent(HookPhase phase,
                                    VMInfo vm,
                                    string label,
                                    int keep,
                                    string snapName,
                                    bool vmState,
                                    double duration,
                                    bool status)
        {
            if (_debug) { _out.WriteLine($"Phase: {PhaseEnumToStr(phase)}"); }
            PhaseEvent?.Invoke(this, new PhaseEventArgs(phase,
                                                        vm,
                                                        label,
                                                        keep,
                                                        snapName,
                                                        vmState,
                                                        duration,
                                                        status));
        }

        /// <summary>
        /// Status auto snapshot.
        /// </summary>
        /// <param name="vmIdsOrNames"></param>
        /// <returns></returns>
        public IEnumerable<Snapshot> Status(string vmIdsOrNames) => Status(vmIdsOrNames, null);

        private IEnumerable<VMInfo> GetVMs(string vmIdsOrNames)
            => _client.GetVMs(vmIdsOrNames).Where(a => a.Status != "unknown");

        /// <summary>
        /// Status auto snapshot.
        /// </summary>
        /// <param name="vmIdsOrNames"></param>
        /// <param name="label"></param>
        public IEnumerable<Snapshot> Status(string vmIdsOrNames, string label)
        {
            //select snapshot and filter
            var snapshots = FilterApp(GetVMs(vmIdsOrNames).SelectMany(a => a.Snapshots));
            return string.IsNullOrWhiteSpace(label) ? snapshots : FilterLabel(snapshots, label);
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
        public ResultSnap Snap(string vmIdsOrNames, string label, int keep, bool state, long timeout)
        {
            _out.WriteLine($@"ACTION Snap
VMs:     {vmIdsOrNames}
Label:   {label}
Keep:    {keep}
State:   {state}
Timeout: {timeout}");

            var ret = new ResultSnap();
            ret.Start();

            CallPhaseEvent(HookPhase.SnapJobStart, null, label, keep, null, state, 0, true);

            foreach (var vm in GetVMs(vmIdsOrNames))
            {
                _out.WriteLine($"----- VM {vm.Id} {vm.Type} -----");

                //exclude template
                if (vm.IsTemplate)
                {
                    _out.WriteLine("Skip VM is template");
                    continue;
                }

                var execSnapVm = new ResultSnapVm
                {
                    VmId = int.Parse(vm.Id)
                };
                ret.Vms.Add(execSnapVm);
                execSnapVm.Start();

                //check agent enabled
                if (vm.Type == VMTypeEnum.Qemu && !((ConfigQemu)vm.Config).AgentEnabled)
                {
                    _out.WriteLine(((ConfigQemu)vm.Config).GetMessageEnablingAgent());
                }

                //create snapshot
                var snapName = GetPrefix(label) + DateTime.Now.ToString(TIMESTAMP_FORMAT);

                CallPhaseEvent(HookPhase.SnapCreatePre, vm, label, keep, snapName, state, 0, true);

                _out.WriteLine($"Create snapshot: {snapName}");

                var inError = true;
                if (!_dryRun)
                {
                    var result = vm.Snapshots.Create(snapName, NAME, state, timeout);
                    inError = result.LogInError(_out);

                    //check error in task
                    var taskStatus = _client.GetExitStatusTask(vm.Node, (result.Response.data as string));
                    if (taskStatus != "OK")
                    {
                        _out.WriteLine($"Error in task: {taskStatus}");
                        inError = true;
                    }
                }

                if (inError)
                {
                    execSnapVm.Stop();
                    CallPhaseEvent(HookPhase.SnapCreateAbort, vm, label, keep, snapName, state, execSnapVm.Elapsed.TotalSeconds, false);
                    continue;
                }

                //remove old snapshot
                if (!SnapshotsRemove(vm, label, keep, timeout))
                {
                    execSnapVm.Stop();
                    continue;
                }

                execSnapVm.Stop();
                execSnapVm.Status = true;

                CallPhaseEvent(HookPhase.SnapCreatePost, vm, label, keep, snapName, state, execSnapVm.Elapsed.TotalSeconds, execSnapVm.Status);

                _out.WriteLine($"VM execution {execSnapVm.Elapsed}");
            }

            ret.Stop();

            CallPhaseEvent(HookPhase.SnapJobEnd, null, label, keep, null, state, ret.Elapsed.TotalSeconds, ret.Status);

            _out.WriteLine($"Total execution {ret.Elapsed}");

            if (_debug) { _out.WriteLine($"Snap Exit: {ret.Status}"); }

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
VMs:     {vmIdsOrNames}
Label:   {label}
Keep:    {keep}
Timeout: {timeout}");

            var watch = new Stopwatch();
            watch.Start();

            var ret = true;
            CallPhaseEvent(HookPhase.CleanJobStart, null, label, keep, null, false, 0, false);

            foreach (var vm in GetVMs(vmIdsOrNames))
            {
                //exclude template
                if (vm.IsTemplate)
                {
                    _out.WriteLine("Skip VM is template");
                    continue;
                }

                _out.WriteLine($"----- VM {vm.Id} {vm.Type} -----");
                if (!SnapshotsRemove(vm, label, keep, timeout)) { ret = false; }
            }

            watch.Stop();
            CallPhaseEvent(HookPhase.CleanJobEnd, null, label, keep, null, false, watch.Elapsed.TotalSeconds, true);

            return ret;
        }

        private bool SnapshotsRemove(VMInfo vm, string label, int keep, long timeout)
        {
            foreach (var snapshot in FilterLabel(vm.Snapshots, label).Reverse().Skip(keep).Reverse())
            {
                var watch = new Stopwatch();
                watch.Start();

                CallPhaseEvent(HookPhase.SnapRemovePre, vm, label, keep, snapshot.Name, false, 0, false);

                _out.WriteLine($"Remove snapshot: {snapshot.Name}");

                var inError = false;
                if (!_dryRun)
                {
                    var result = vm.Snapshots.Remove(snapshot, timeout);
                    inError = result.LogInError(_out);

                    //check error in task
                    var taskStatus = _client.GetExitStatusTask(vm.Node, (result.Response.data as string));
                    if (taskStatus != "OK")
                    {
                        _out.WriteLine($"Error in task: {taskStatus}");
                        inError = true;
                    }
                }

                watch.Stop();
                if (inError)
                {
                    if (_debug) { _out.WriteLine($"Snap remove: problem in remove "); }

                    CallPhaseEvent(HookPhase.SnapRemoveAbort, vm, label, keep, snapshot.Name, false, watch.Elapsed.TotalSeconds, false);
                    return false;
                }

                CallPhaseEvent(HookPhase.SnapRemovePost, vm, label, keep, snapshot.Name, false, watch.Elapsed.TotalSeconds, true);
            }

            return true;
        }
    }
}