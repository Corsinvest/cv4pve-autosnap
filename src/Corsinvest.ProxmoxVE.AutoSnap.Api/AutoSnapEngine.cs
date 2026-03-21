/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Microsoft.Extensions.Logging;

namespace Corsinvest.ProxmoxVE.AutoSnap.Api;

/// <summary>
/// AutoSnap engine.
/// </summary>
/// <remarks>
/// Constructor command
/// </remarks>
/// <param name="client"></param>
/// <param name="loggerFactory"></param>
/// <param name="out"></param>
/// <param name="dryRun"></param>
public class AutoSnapEngine(PveClient client, ILoggerFactory loggerFactory, TextWriter @out, bool dryRun)
{
    private readonly ILogger<AutoSnapEngine> _logger = loggerFactory.CreateLogger<AutoSnapEngine>();

    /// <summary>
    /// Permissions request
    /// </summary>
    /// <value></value>
    public IEnumerable<string> Permissions { get; } = ["VM.Audit", "VM.Snapshot", "Datastore.Audit", "Pool.Allocate"];

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
        return tmsLen + prfLen < name.Length ? name[prfLen..^tmsLen] : string.Empty;
    }

    /// <summary>
    /// Event phase.
    /// </summary>
    public event Func<PhaseEventArgs, Task>? PhaseEvent;

    /// <summary>
    /// Phases
    /// </summary>
    /// <value></value>
    public static IReadOnlyDictionary<string, HookPhase> Phases { get; } = new Dictionary<string, HookPhase>
    {
        ["clean-job-start"] = HookPhase.CleanJobStart,
        ["clean-job-end"] = HookPhase.CleanJobEnd,
        ["snap-job-start"] = HookPhase.SnapJobStart,
        ["snap-job-end"] = HookPhase.SnapJobEnd,
        ["snap-create-pre"] = HookPhase.SnapCreatePre,
        ["snap-create-post"] = HookPhase.SnapCreatePost,
        ["snap-create-abort"] = HookPhase.SnapCreateAbort,
        ["snap-remove-pre"] = HookPhase.SnapRemovePre,
        ["snap-remove-post"] = HookPhase.SnapRemovePost,
        ["snap-remove-abort"] = HookPhase.SnapRemoveAbort,
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

    private async Task CallPhaseEventAsync(PhaseEventArgs args)
    {
        _logger.LogDebug("Phase: {phase}", PhaseEnumToStr(args.Phase));

        if (PhaseEvent is null) { return; }

        foreach (var handler in PhaseEvent.GetInvocationList().Cast<Func<PhaseEventArgs, Task>>())
        {
            try { await handler(args); }
            catch (Exception ex) { _logger.LogError(ex, ex.Message); }
        }
    }

    private async Task<IEnumerable<IClusterResourceVm>> GetVmsAsync(string vmIdsOrNames)
        => (await client.GetVmsAsync(vmIdsOrNames)).Where(a => !a.IsUnknown);

    /// <summary>
    /// Status auto snapshot.
    /// </summary>
    /// <param name="vmIdsOrNames"></param>
    /// <param name="label"></param>
    /// <param name="timestampFormat"></param>
    public async Task<IReadOnlyDictionary<IClusterResourceVm, IEnumerable<VmSnapshot>>> StatusAsync(string vmIdsOrNames, string label, string timestampFormat)
    {
        timestampFormat = GetTimestampFormat(timestampFormat);

        var ret = new Dictionary<IClusterResourceVm, IEnumerable<VmSnapshot>>();

        foreach (var vm in await GetVmsAsync(vmIdsOrNames))
        {
            var snapshots = FilterApp(await SnapshotHelper.GetSnapshotsAsync(client, vm.Node, vm.VmType, vm.VmId));
            if (!string.IsNullOrWhiteSpace(label)) { snapshots = FilterLabel(snapshots, label, timestampFormat); }
            ret.Add(vm, snapshots);
        }
        return ret;
    }

    private static IEnumerable<VmSnapshot> FilterApp(IEnumerable<VmSnapshot> snapshots)
        => snapshots.Where(a => (a.Description + string.Empty).Replace("\n", string.Empty) == Name
                                || (a.Description + string.Empty).Replace("\n", string.Empty) == OldName);

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
    /// <param name="onlyRuns"></param>
    /// <returns></returns>
    public async Task<ResultSnap> SnapAsync(string vmIdsOrNames,
                                            string label,
                                            int keep,
                                            bool state,
                                            long timeout,
                                            string timestampFormat,
                                            int maxPercentageStorage,
                                            bool onlyRuns)
    {
        timestampFormat = GetTimestampFormat(timestampFormat);
        var pveFullVersion = (await client.Version.Version()).ToData().version as string;
        var pveVersion = double.Parse(pveFullVersion!.Split(".")[0]);

        @out.WriteLine($@"ACTION Snap
PVE Version:      {pveFullVersion}
VMs:              {vmIdsOrNames}
Label:            {label}
Keep:             {keep}
State:            {state}
Only running:     {onlyRuns}
Timeout:          {Math.Round(timeout / 1000.0, 1)} sec.
Timestamp format: {timestampFormat}
Max % Storage :   {maxPercentageStorage}%");

        var snapName = GetPrefix(label) + DateTime.Now.ToString(timestampFormat);
        var ret = new ResultSnap
        {
            SnapName = snapName
        };
        ret.Start();

        await CallPhaseEventAsync(new(HookPhase.SnapJobStart, null, label, keep, null, state, 0, true));

        var storagesCheck = new Dictionary<string, bool>();
        var storagesPrint = new List<object[]>();

        var vms = await GetVmsAsync(vmIdsOrNames);
        if (!vms.Any())
        {
            @out.WriteLine($"----- VMs with '{vmIdsOrNames}' NOT FOUND -----");
            @out.WriteLine($"----- POSSIBLE PROBLEM PERMISSION 'VM.Audit' -----");
        }

        var nodes = vms.Select(a => a.Node).Distinct().ToList();

        var checkStorage = pveVersion >= 6;

        if (checkStorage)
        {
            var contentAllowed = new[] { "images", "rootdir" };

            var storages = (await client.GetStoragesAsync())
                                .Where(a => !a.IsUnknown && nodes.Contains(a.Node))
                                .ToList();

            if (storages.Exists(a => string.IsNullOrWhiteSpace(a.Content)))
            {
                //content not exists
                //found in nodes/storages
                foreach (var node in nodes)
                {
                    var nodeStorages = await client.Nodes[node].Storage.GetAsync(string.Join(",", contentAllowed));

                    foreach (var storage in storages.Where(a => a.Node == node))
                    {
                        storage.Content = nodeStorages.FirstOrDefault(a => a.Storage == storage.Storage
                                                                            && a.Type == storage.PluginType)
                                                      ?.Content ?? string.Empty;
                    }
                }
            }

            storages = storages.Where(a => a.Content.Split(',')
                               .Any(a => contentAllowed.Contains(a)))
                               .OrderBy(a => a.Node)
                               .ThenBy(a => a.Storage)
                               .ToList();

            if (storages.Count == 0) { @out.WriteLine($"----- POSSIBLE PROBLEM PERMISSION 'Datastore.Audit' -----"); }

            //check storage capacity
            foreach (var storage in storages)
            {
                var valid = !(storage.DiskUsage == 0
                                || storage.DiskSize == 0
                                || storage.DiskUsagePercentage > maxPercentageStorage);

                var key = $"{storage.Node}/{storage.Storage}";
                storagesPrint.Add(item:
                [
                    key,
                    storage.PluginType,
                    valid? "Ok": "Ko",
                    Math.Round(storage.DiskUsagePercentage * 100, 1),
                    FormatHelper.FromBytes(storage.DiskSize),
                    FormatHelper.FromBytes(storage.DiskUsage),
                ]);

                storagesCheck.Add(key, valid);
            }

            if (storagesPrint.Count != 0)
            {
                var size = new[] { 25, 10, 10, 10, 12, 12 };

                string FormatLine(object[] values)
                {
                    var ret = new StringBuilder();
                    for (int i = 0; i < size.Length; i++) { ret.Append((values[i] + string.Empty).PadLeft(size[i])); }
                    return ret.ToString();
                }

                @out.WriteLine(FormatLine(["Storage", "Type", "Valid", "Used % ", "Disk Size", "Disk Usage"]));
                foreach (var item in storagesPrint) { @out.WriteLine(FormatLine(item)); }
            }
        }
        else
        {
            @out.WriteLine($"The Proxmox VE version {pveFullVersion} does not verify the storage % usage!");
        }

        foreach (var vm in vms)
        {
            @out.WriteLine($"----- VM {vm.VmId} {vm.Type} {vm.Status} -----");

            if (!vm.IsRunning && onlyRuns)
            {
                @out.WriteLine("Skip VM '--only-running' parameter used!");
                continue;
            }

            //exclude template
            if (vm.IsTemplate)
            {
                @out.WriteLine("Skip VM is template");
                continue;
            }

            VmConfig vmConfig = vm.VmType switch
            {
                VmType.Qemu => await client.Nodes[vm.Node].Qemu[vm.VmId].Config.GetAsync(),
                VmType.Lxc => await client.Nodes[vm.Node].Lxc[vm.VmId].Config.GetAsync(),
                _ => throw new InvalidEnumArgumentException(),
            };

            var resultSnapVm = new ResultSnapVm
            {
                VmId = vm.VmId
            };
            ret.Vms.Add(resultSnapVm);
            resultSnapVm.Start();

            //check agent enabled
            if (vm.VmType == VmType.Qemu && !((VmConfigQemu)vmConfig).AgentEnabled)
            {
                @out.WriteLine($"VM {vm.VmId} consider enabling QEMU agent see https://pve.proxmox.com/wiki/Qemu-guest-agent");
            }

            if (checkStorage && vmConfig.Disks.Any())
            {
                //verify storage - check only Proxmox managed storages, ignore bind mounts
                var validStorage = true;
                foreach (var item in vmConfig.Disks)
                {
                    // Check only if storage exists in storagesCheck (Proxmox managed storage)
                    // If not found (e.g., bind mount directory), ignore it
                    if (storagesCheck.TryGetValue($"{vm.Node}/{item.Storage}", out var isValid) && !isValid)
                    {
                        validStorage = false;
                        break;
                    }
                }

                if (!validStorage)
                {
                    @out.WriteLine($"Skip VM problem storage space out of {maxPercentageStorage}%");
                    resultSnapVm.Stop();
                    continue;
                }
            }

            //create snapshot
            await CallPhaseEventAsync(new(HookPhase.SnapCreatePre, vm, label, keep, snapName, state, 0, true));

            @out.WriteLine($"Create snapshot: {snapName}");

            var inError = false;
            if (!dryRun)
            {
                try
                {
                    var result = await SnapshotHelper.CreateSnapshotAsync(client,
                                                                          vm.Node,
                                                                          vm.VmType,
                                                                          vm.VmId,
                                                                          snapName,
                                                                          Name,
                                                                          state,
                                                                          timeout);

                    inError = await CheckResultAsync(result);
                }
                catch (Exception ex)
                {
                    inError = true;
                    _logger.LogError(ex, ex.Message);
                    @out.WriteLine(ex.Message);
                }
            }

            if (inError)
            {
                resultSnapVm.Stop();
                await CallPhaseEventAsync(new(HookPhase.SnapCreateAbort, vm, label, keep, snapName, state, resultSnapVm.Elapsed.TotalSeconds, false));
                continue;
            }

            //remove old snapshot
            if (!await SnapshotsRemoveAsync(vm, label, keep, timeout, timestampFormat))
            {
                resultSnapVm.Stop();
                continue;
            }

            resultSnapVm.Stop();
            resultSnapVm.Status = true;

            await CallPhaseEventAsync(new(HookPhase.SnapCreatePost, vm, label, keep, snapName, state, resultSnapVm.Elapsed.TotalSeconds, resultSnapVm.Status));

            @out.WriteLine($"VM execution {resultSnapVm.Elapsed}");
        }

        ret.Stop();

        await CallPhaseEventAsync(new(HookPhase.SnapJobEnd, null, label, keep, null, state, ret.Elapsed.TotalSeconds, ret.Status));

        @out.WriteLine($"Total execution {ret.Elapsed}");

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
    public async Task<bool> CleanAsync(string vmIdsOrNames, string label, int keep, long timeout, string timestampFormat)
    {
        timestampFormat = GetTimestampFormat(timestampFormat);

        @out.WriteLine($@"ACTION Clean
VMs:              {vmIdsOrNames}
Label:            {label}
Keep:             {keep}
Timeout:          {Math.Round(timeout / 1000.0, 1)} sec.
Timestamp format: {timestampFormat}");

        var watch = new Stopwatch();
        watch.Start();

        var ret = true;
        await CallPhaseEventAsync(new(HookPhase.CleanJobStart, null, label, keep, null, false, 0, false));

        foreach (var vm in await GetVmsAsync(vmIdsOrNames))
        {
            //exclude template
            if (vm.IsTemplate)
            {
                @out.WriteLine("Skip VM is template");
                continue;
            }

            @out.WriteLine($"----- VM {vm.VmId} {vm.Type} -----");
            if (!await SnapshotsRemoveAsync(vm, label, keep, timeout, timestampFormat)) { ret = false; }
        }

        watch.Stop();
        await CallPhaseEventAsync(new(HookPhase.CleanJobEnd, null, label, keep, null, false, watch.Elapsed.TotalSeconds, ret));

        return ret;
    }

    private async Task<bool> CheckResultAsync(Result result)
    {
        var inError = result.InError();
        if (inError) { @out.WriteLine(result.GetError()); }

        //check error in task
        if (await client.TaskIsRunningAsync(result.ToData<string>()))
        {
            @out.WriteLine($"Error task in run... increase the timeout!");
            inError = true;
        }
        else
        {
            var taskStatus = await client.GetExitStatusTaskAsync(result.ToData<string>());
            if (taskStatus != "OK")
            {
                @out.WriteLine($"Error in task: {taskStatus}");
                inError = true;
            }
        }

        return inError;
    }

    private async Task<bool> SnapshotsRemoveAsync(IClusterResourceVm vm,
                                                  string label,
                                                  int keep,
                                                  long timeout,
                                                  string timestampFormat)
    {
        foreach (var snapshot in FilterLabel(await SnapshotHelper.GetSnapshotsAsync(client, vm.Node, vm.VmType, vm.VmId),
                                             label,
                                             timestampFormat).OrderByDescending(a => a.Name).Skip(keep).OrderBy(a => a.Name))
        {
            var watch = Stopwatch.StartNew();

            await CallPhaseEventAsync(new(HookPhase.SnapRemovePre, vm, label, keep, snapshot.Name, false, 0, false));

            @out.WriteLine($"Remove snapshot: {snapshot.Name}");

            var inError = false;
            if (!dryRun)
            {
                try
                {
                    var result = await SnapshotHelper.RemoveSnapshotAsync(client,
                                                                          vm.Node,
                                                                          vm.VmType,
                                                                          vm.VmId,
                                                                          snapshot.Name,
                                                                          timeout,
                                                                          true);
                    inError = await CheckResultAsync(result);
                }
                catch (Exception ex)
                {
                    inError = true;
                    _logger.LogError(ex, ex.Message);
                    @out.WriteLine(ex.Message);
                }
            }

            watch.Stop();
            if (inError)
            {
                _logger.LogWarning("Snap remove: problem in remove");

                await CallPhaseEventAsync(new(HookPhase.SnapRemoveAbort, vm, label, keep, snapshot.Name, false, watch.Elapsed.TotalSeconds, false));
                return false;
            }

            await CallPhaseEventAsync(new(HookPhase.SnapRemovePost, vm, label, keep, snapshot.Name, false, watch.Elapsed.TotalSeconds, true));
        }

        return true;
    }
}