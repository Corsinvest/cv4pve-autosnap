/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: 2019 Copyright Corsinvest Srl
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Microsoft.Extensions.Logging;

namespace Corsinvest.ProxmoxVE.AutoSnap.Api
{
    /// <summary>
    /// Command autosnap.
    /// </summary>
    public class Application
    {
        private readonly ILogger<Application> _logger;

        private static readonly string Prefix = "auto";

        /// <summary>
        /// Default time stamp format
        /// </summary>
        public static readonly string DefaultTimestampFormat = "yyMMddHHmmss";

        /// <summary>
        /// Application name
        /// </summary>
        public static readonly string Name = "cv4pve-autosnap";

        /// <summary>
        /// Old application name
        /// </summary>
        private static readonly string OldName = "eve4pve-autosnap";

        private readonly PveClient _client;
        private readonly bool _dryRun;
        private readonly TextWriter _out;

        /// <summary>
        /// Constructor command
        /// </summary>
        /// <param name="client"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="out"></param>
        /// <param name="dryRun"></param>
        public Application(PveClient client, ILoggerFactory loggerFactory, TextWriter @out, bool dryRun)
        {
            _client = client;
            _dryRun = dryRun;
            _out = @out;
            _logger = loggerFactory.CreateLogger<Application>();
        }

        private static string GetTimestampFormat(string timestampFormat)
            => string.IsNullOrWhiteSpace(timestampFormat)
                ? DefaultTimestampFormat
                : timestampFormat;

        /// <summary>
        /// Get label from description
        /// </summary>
        /// <param name="name"></param>
        /// <param name="timestampFormat"></param>
        /// <returns></returns>
        public static string GetLabelFromName(string name, string timestampFormat)
        {
            var tmsLen = GetTimestampFormat(timestampFormat).Length;
            var prfLen = Prefix.Length;
            return tmsLen + prfLen < name.Length ? name[prfLen..^tmsLen] : "";
        }

        /// <summary>
        /// Event phase.
        /// </summary>
        public event EventHandler<PhaseEventArgs> PhaseEvent;

        /// <summary>
        /// Phases
        /// </summary>
        /// <value></value>
        public static readonly Dictionary<string, HookPhase> Phases = new()
        {
            { "clean-job-start", HookPhase.CleanJobStart },
            { "clean-job-end", HookPhase.CleanJobEnd },
            { "snap-job-start", HookPhase.SnapJobStart },
            { "snap-job-end", HookPhase.SnapJobEnd },
            { "snap-create-pre", HookPhase.SnapCreatePre },
            { "snap-create-post", HookPhase.SnapCreatePost },
            { "snap-create-abort", HookPhase.SnapCreateAbort },
            { "snap-remove-pre", HookPhase.SnapRemovePre },
            { "snap-remove-post", HookPhase.SnapRemovePost },
            { "snap-remove-abort", HookPhase.SnapRemoveAbort }
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
                                    IClusterResourceVm vm,
                                    string label,
                                    int keep,
                                    string snapName,
                                    bool vmState,
                                    double duration,
                                    bool status)
        {
            _logger.LogDebug($"Phase: {PhaseEnumToStr(phase)}");
            PhaseEvent?.Invoke(this, new PhaseEventArgs(phase,
                                                        vm,
                                                        label,
                                                        keep,
                                                        snapName,
                                                        vmState,
                                                        duration,
                                                        status));
        }

        private async Task<IEnumerable<IClusterResourceVm>> GetVms(string vmIdsOrNames)
            => (await _client.GetVms(vmIdsOrNames)).Where(a => !a.IsUnknown);

        /// <summary>
        /// Status auto snapshot.
        /// </summary>
        /// <param name="vmIdsOrNames"></param>
        /// <param name="label"></param>
        /// <param name="timestampFormat"></param>
        public async Task<IReadOnlyDictionary<IClusterResourceVm, IEnumerable<VmSnapshot>>> Status(string vmIdsOrNames, string label, string timestampFormat)
        {
            timestampFormat = GetTimestampFormat(timestampFormat);

            var ret = new Dictionary<IClusterResourceVm, IEnumerable<VmSnapshot>>();

            foreach (var vm in await GetVms(vmIdsOrNames))
            {
                var snapshots = FilterApp(await SnapshotHelper.GetSnapshots(_client, vm.Node, vm.VmType, vm.VmId));
                if (!string.IsNullOrWhiteSpace(label)) { snapshots = FilterLabel(snapshots, label, timestampFormat); }
                ret.Add(vm, snapshots);
            }
            return ret;
        }

        private static IEnumerable<VmSnapshot> FilterApp(IEnumerable<VmSnapshot> snapshots)
            => snapshots.Where(a => a.Description == Name || a.Description == OldName);

        private static string GetPrefix(string label) => Prefix + label;

        private static IEnumerable<VmSnapshot> FilterLabel(IEnumerable<VmSnapshot> snapshots, string label, string timestampFormat)
        {
            var lenTms = GetTimestampFormat(timestampFormat).Length;
            return FilterApp(snapshots.Where(a => (a.Name.Length - lenTms) > 0 && a.Name[..^lenTms] == GetPrefix(label)));
        }

        /// <summary>
        /// Execute a autosnap.
        /// </summary>
        /// <param name="vmIdsOrNames"></param>
        /// <param name="label"></param>
        /// <param name="keep"></param>
        /// <param name="state"></param>
        /// <param name="timeout"></param>
        /// <param name="timestampFormat"></param>
        /// <param name="maxPercentageStorage"></param>
        /// <returns></returns>
        public async Task<ResultSnap> Snap(string vmIdsOrNames,
                                           string label,
                                           int keep,
                                           bool state,
                                           long timeout,
                                           string timestampFormat,
                                           int maxPercentageStorage)
        {
            timestampFormat = GetTimestampFormat(timestampFormat);

            _out.WriteLine($@"ACTION Snap
VMs:              {vmIdsOrNames}
Label:            {label}
Keep:             {keep}
State:            {state}
Timeout:          {Math.Round(timeout / 1000.0, 1)} sec.
Timestamp format: {timestampFormat}
Max % Storage :   {maxPercentageStorage}%");

            var ret = new ResultSnap();
            ret.Start();

            CallPhaseEvent(HookPhase.SnapJobStart, null, label, keep, null, state, 0, true);

            var storagesCheck = new Dictionary<string, bool>();
            var storagesPrint = new List<object[]>();

            var vms = await GetVms(vmIdsOrNames);
            if (!vms.Any()) { _out.WriteLine($"----- POSSIBLE PROBLEM PERMISSION 'VM.Audit' -----"); }

            var nodes = vms.Select(a => a.Node).Distinct().ToList();

            var contentAllowed = new[] { "images", "rootdir" };
            var storages = (await _client.GetStorages())
                                .Where(a => !a.IsUnknown
                                            && nodes.Contains(a.Node)
                                            && a.Content.Split(',').Any(a => contentAllowed.Contains(a)))
                                .OrderBy(a => a.Node)
                                .ThenBy(a => a.Storage);

            if (!storages.Any()) { _out.WriteLine($"----- POSSIBLE PROBLEM PERMISSION 'Datastore.Audit' -----"); }

            //check storage capacity
            foreach (var storage in storages)
            {
                var valid = !(storage.DiskUsage == 0
                                || storage.DiskSize == 0
                                || storage.DiskUsagePercentage > maxPercentageStorage);

                var key = $"{storage.Node}/{storage.Storage}";
                storagesPrint.Add(item: new object[]
                {
                    key,
                    storage.PluginType,
                    valid? "Ok": "Ko",
                    Math.Round(storage.DiskUsagePercentage * 100,1),
                    FormatHelper.FromBytes(storage.DiskSize),
                    FormatHelper.FromBytes(storage.DiskUsage),
                });

                storagesCheck.Add(key, valid);
            }

            if (storagesPrint.Any())
            {
                var size = new[] { 25, 10, 10, 10, 12, 12 };

                string FormatLine(object[] values)
                {
                    var ret = new StringBuilder();
                    for (int i = 0; i < size.Length; i++) { ret.Append((values[i] + "").PadLeft(size[i])); }
                    return ret.ToString();
                }

                _out.WriteLine(FormatLine(new[] { "Storage", "Type", "Valid", "Used % ", "Disk Size", "Disk Usage" }));
                foreach (var item in storagesPrint) { _out.WriteLine(FormatLine(item)); }
            }

            foreach (var vm in vms)
            {
                _out.WriteLine($"----- VM {vm.VmId} {vm.Type} -----");

                //exclude template
                if (vm.IsTemplate)
                {
                    _out.WriteLine("Skip VM is template");
                    continue;
                }

                VmConfig vmConfig = vm.VmType switch
                {
                    VmType.Qemu => await _client.Nodes[vm.Node].Qemu[vm.VmId].Config.Get(),
                    VmType.Lxc => await _client.Nodes[vm.Node].Lxc[vm.VmId].Config.Get(),
                    _ => throw new InvalidEnumArgumentException(),
                };

                var execSnapVm = new ResultSnapVm
                {
                    VmId = vm.VmId
                };
                ret.Vms.Add(execSnapVm);
                execSnapVm.Start();

                //check agent enabled
                if (vm.VmType == VmType.Qemu && !((VmConfigQemu)vmConfig).AgentEnabled)
                {
                    _out.WriteLine($"VM {vm.VmId} consider enabling QEMU agent see https://pve.proxmox.com/wiki/Qemu-guest-agent");
                }

                //verify storage
                var validStorage = false;
                foreach (var item in vmConfig.Disks)
                {
                    validStorage = false;
                    storagesCheck.TryGetValue($"{vm.Node}/{item.Storage}", out validStorage);
                    if (!validStorage) { break; }
                }

                if (!validStorage)
                {
                    _out.WriteLine($"Skip VM problem storage space out of {maxPercentageStorage}%");
                    execSnapVm.Stop();
                    continue;
                }

                //create snapshot
                var snapName = GetPrefix(label) + DateTime.Now.ToString(timestampFormat);

                CallPhaseEvent(HookPhase.SnapCreatePre, vm, label, keep, snapName, state, 0, true);

                _out.WriteLine($"Create snapshot: {snapName}");

                var inError = true;
                if (!_dryRun)
                {
                    var result = await SnapshotHelper.CreateSnapshot(_client,
                                                                     vm.Node,
                                                                     vm.VmType,
                                                                     vm.VmId,
                                                                     snapName,
                                                                     Name,
                                                                     state,
                                                                     timeout);


                    inError = result.InError();
                    if (inError) { _out.WriteLine(result.GetError()); }

                    //check error in task
                    var taskStatus = await _client.GetExitStatusTask(result.ToData<string>());
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
                if (!await SnapshotsRemove(vm, label, keep, timeout, timestampFormat))
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

            _logger.LogDebug($"Snap Exit: {ret.Status}");

            return ret;
        }

        /// <summary>
        /// Clean autosnap.
        /// </summary>
        /// <param name="vmIdsOrNames"></param>
        /// <param name="label"></param>
        /// <param name="keep"></param>
        /// <param name="timeout"></param>
        /// <param name="timestampFormat"></param>
        /// <returns></returns>
        public async Task<bool> Clean(string vmIdsOrNames, string label, int keep, long timeout, string timestampFormat)
        {
            timestampFormat = GetTimestampFormat(timestampFormat);

            _out.WriteLine($@"ACTION Clean
VMs:              {vmIdsOrNames}
Label:            {label}
Keep:             {keep}
Timeout:          {Math.Round(timeout / 1000.0, 1)} sec.
Timestamp format: {timestampFormat}");

            var watch = new Stopwatch();
            watch.Start();

            var ret = true;
            CallPhaseEvent(HookPhase.CleanJobStart, null, label, keep, null, false, 0, false);

            foreach (var vm in await GetVms(vmIdsOrNames))
            {
                //exclude template
                if (vm.IsTemplate)
                {
                    _out.WriteLine("Skip VM is template");
                    continue;
                }

                _out.WriteLine($"----- VM {vm.VmId} {vm.Type} -----");
                if (!await SnapshotsRemove(vm, label, keep, timeout, timestampFormat)) { ret = false; }
            }

            watch.Stop();
            CallPhaseEvent(HookPhase.CleanJobEnd, null, label, keep, null, false, watch.Elapsed.TotalSeconds, true);

            return ret;
        }

        private async Task<bool> SnapshotsRemove(IClusterResourceVm vm,
                                                 string label,
                                                 int keep,
                                                 long timeout,
                                                 string timstampFormat)
        {
            foreach (var snapshot in FilterLabel(await SnapshotHelper.GetSnapshots(_client, vm.Node, vm.VmType, vm.VmId),
                                                 label,
                                                 timstampFormat).Reverse().Skip(keep).Reverse())
            {
                var watch = new Stopwatch();
                watch.Start();

                CallPhaseEvent(HookPhase.SnapRemovePre, vm, label, keep, snapshot.Name, false, 0, false);

                _out.WriteLine($"Remove snapshot: {snapshot.Name}");

                var inError = false;
                if (!_dryRun)
                {
                    var result = await SnapshotHelper.RemoveSnapshot(_client, vm.Node, vm.VmType, vm.VmId, snapshot.Name, timeout);
                    inError = result.InError();
                    if (inError) { _out.WriteLine(result.GetError()); }

                    //check error in task
                    var taskStatus = await _client.GetExitStatusTask(result.ToData<string>());
                    if (taskStatus != "OK")
                    {
                        _out.WriteLine($"Error in task: {taskStatus}");
                        inError = true;
                    }
                }

                watch.Stop();
                if (inError)
                {
                    _logger.LogWarning($"Snap remove: problem in remove ");

                    CallPhaseEvent(HookPhase.SnapRemoveAbort,
                                   vm,
                                   label,
                                   keep,
                                   snapshot.Name,
                                   false,
                                   watch.Elapsed.TotalSeconds,
                                   false);
                    return false;
                }

                CallPhaseEvent(HookPhase.SnapRemovePost,
                               vm,
                               label,
                               keep,
                               snapshot.Name,
                               false,
                               watch.Elapsed.TotalSeconds,
                               true);
            }

            return true;
        }
    }
}