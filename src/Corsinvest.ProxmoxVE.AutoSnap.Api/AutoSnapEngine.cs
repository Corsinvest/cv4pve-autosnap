/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
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
public partial class AutoSnapEngine(PveClient client, ILoggerFactory loggerFactory, TextWriter @out, bool dryRun)
{
    private readonly ILogger<AutoSnapEngine> _logger = loggerFactory.CreateLogger<AutoSnapEngine>();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Phase: {Phase}")]
    private partial void LogPhase(string phase);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error")]
    private partial void LogUnexpectedError(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Snap remove: problem in remove")]
    private partial void LogSnapRemoveProblem();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Snap exit: {Status}")]
    private partial void LogSnapExit(bool status);

    /// <summary>
    /// Permissions request
    /// </summary>
    /// <value></value>
    public IEnumerable<string> Permissions { get; } = ["VM.Audit", "VM.Snapshot", "Datastore.Audit", "Pool.Audit"];

    private const string Prefix = "auto";

    /// <summary>
    /// Default time stamp format
    /// </summary>
    public static readonly string DefaultTimestampFormat = "yyMMddHHmmss";

    /// <summary>
    /// Application name
    /// </summary>
    public static readonly string Name = "cv4pve-autosnap";

    private static string GetTimestampFormat(string timestampFormat)
        => string.IsNullOrWhiteSpace(timestampFormat)
            ? DefaultTimestampFormat
            : timestampFormat;

    /// <summary>
    /// Maximum length of a snapshot name accepted by Proxmox VE.
    /// </summary>
    public const int MaxSnapshotNameLength = 40;

    /// <summary>
    /// Check that the label and the timestamp format give snapshot names Proxmox VE accepts and the tool
    /// can read back: the written timestamp as long as the format string, names in time order when sorted,
    /// only letters, digits, '-' and '_', at most <see cref="MaxSnapshotNameLength"/> characters.
    /// </summary>
    /// <param name="label">Label; empty checks the timestamp format only.</param>
    /// <param name="timestampFormat">Timestamp format; empty for the default.</param>
    /// <returns>The problem found, or null.</returns>
    public static string? ValidateLabelAndTimestampFormat(string label, string timestampFormat)
    {
        timestampFormat = GetTimestampFormat(timestampFormat);

        //dates across every unit, and across the 12-hour clock, in time order
        var start = new DateTime(2026, 1, 1, 1, 1, 1);
        DateTime[] dates = [start, start.AddSeconds(9), start.AddMinutes(9), start.AddHours(9), start.AddHours(12),
                            start.AddDays(9), start.AddMonths(9), start.AddMonths(11).AddDays(30).AddHours(22), start.AddYears(1)];

        string[] timestamps;
        try { timestamps = [.. dates.Select(a => a.ToString(timestampFormat, CultureInfo.InvariantCulture))]; }
        catch (FormatException) { return $"Timestamp format '{timestampFormat}' is not valid."; }

        if (timestamps.Any(a => a.Length != timestampFormat.Length))
        {
            return $"Timestamp format '{timestampFormat}' writes timestamps of another length than the format: use fixed-width specifiers (yyyy, yy, MM, dd, HH, mm, ss) and no quoted text.";
        }

        for (var i = 1; i < timestamps.Length; i++)
        {
            if (string.CompareOrdinal(timestamps[i - 1], timestamps[i]) > 0)
            {
                return $"Timestamp format '{timestampFormat}' does not sort in time order: write the largest unit first (year, month, day, hour, minute, second) and use HH, not hh.";
            }
        }

        var name = GetPrefix(label) + timestamps[0];
        if (!ValidName().IsMatch(name))
        {
            return $"Snapshot name '{name}' is not valid: label and timestamp format may contain only letters, digits, '-' and '_'.";
        }

        if (name.Length > MaxSnapshotNameLength)
        {
            return $"Snapshot name '{name}' is {name.Length} characters long, Proxmox VE accepts at most {MaxSnapshotNameLength}: use a shorter label or timestamp format.";
        }

        return null;
    }

    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_-]+$")]
    private static partial Regex ValidName();

    private static void CheckLabelAndTimestampFormat(string label, string timestampFormat)
    {
        var error = ValidateLabelAndTimestampFormat(label, timestampFormat);
        if (error != null) { throw new ArgumentException(error); }
    }

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
    /// Phase enum to string
    /// </summary>
    /// <param name="phase"></param>
    /// <returns></returns>
    public static string PhaseEnumToStr(HookPhase phase) => Phases.SingleOrDefault(a => a.Value == phase).Key;

    private async Task CallPhaseEventAsync(PhaseEventArgs args)
    {
        LogPhase(PhaseEnumToStr(args.Phase));

        if (PhaseEvent is null) { return; }

        foreach (var handler in PhaseEvent.GetInvocationList().Cast<Func<PhaseEventArgs, Task>>())
        {
            try { await handler(args); }
            catch (Exception ex) { LogUnexpectedError(ex); }
        }
    }

    private void WriteVmsNotFound(string vmIdsOrNames)
    {
        @out.WriteLine($"----- VMs with '{vmIdsOrNames}' NOT FOUND -----");
        @out.WriteLine("----- POSSIBLE PROBLEM PERMISSION 'VM.Audit' -----");

        //pool selection needs Pool.Audit to read GET /pools (Pool.Allocate on PVE 8 and earlier)
        if (vmIdsOrNames.Contains("@pool-"))
        {
            @out.WriteLine("----- POSSIBLE PROBLEM PERMISSION 'Pool.Audit' (PVE 9+) / 'Pool.Allocate' (PVE 8 and earlier) -----");
        }
    }

    private async Task<List<IClusterResourceVm>> GetVmsAsync(string vmIdsOrNames)
        => [.. await client.GetVmsAsync(vmIdsOrNames)];

    private const string SkipUnknown = "Skip VM status unknown: node offline or not reachable";

    /// <summary>
    /// Disks and mount points Proxmox VE cannot snapshot: devices ('/dev/...') and, for containers, bind mounts.
    /// Proxmox VE checks every volume for a snapshot, 'backup=0' included, so any of them refuses it.
    /// </summary>
    internal static IEnumerable<VmDisk> GetNotSnapshottableDisks(IEnumerable<VmDisk> disks)
        => disks.Where(a => !a.IsUnused && (a.Passthrough || !string.IsNullOrEmpty(a.MountSourcePath)));

    /// <summary>
    /// Status auto snapshot.
    /// </summary>
    /// <param name="vmIdsOrNames"></param>
    /// <param name="label"></param>
    /// <param name="timestampFormat"></param>
    public async Task<IReadOnlyDictionary<IClusterResourceVm, IEnumerable<VmSnapshot>>> StatusAsync(string vmIdsOrNames,
                                                                                                    string label,
                                                                                                    string timestampFormat)
    {
        timestampFormat = GetTimestampFormat(timestampFormat);
        CheckLabelAndTimestampFormat(label ?? "", timestampFormat);

        var ret = new Dictionary<IClusterResourceVm, IEnumerable<VmSnapshot>>();

        foreach (var vm in (await GetVmsAsync(vmIdsOrNames)).Where(a => !a.IsUnknown))
        {
            var snapshots = FilterApp(await SnapshotHelper.GetSnapshotsAsync(client, vm.Node, vm.VmType, vm.VmId));
            if (!string.IsNullOrWhiteSpace(label)) { snapshots = FilterLabel(snapshots, label, timestampFormat); }
            ret.Add(vm, snapshots);
        }
        return ret;
    }

    internal static IEnumerable<VmSnapshot> FilterApp(IEnumerable<VmSnapshot> snapshots)
        => snapshots.Where(a => (a.Description + "").Replace("\n", "") == Name);

    private static string GetPrefix(string label) => Prefix + label;

    /// <summary>
    /// Name of the snapshot of <paramref name="label"/> taken at <paramref name="date"/>. The timestamp uses the
    /// invariant culture: with the culture of the host a Thai, Persian or Hijri calendar would change the year.
    /// </summary>
    internal static string GetSnapName(string label, string timestampFormat, DateTime date)
        => GetPrefix(label) + date.ToString(GetTimestampFormat(timestampFormat), CultureInfo.InvariantCulture);

    internal static IEnumerable<VmSnapshot> FilterLabel(IEnumerable<VmSnapshot> snapshots, string label, string timestampFormat)
    {
        var lenTms = GetTimestampFormat(timestampFormat).Length;
        return FilterApp(snapshots.Where(a => (a.Name.Length > lenTms) && a.Name[..^lenTms] == GetPrefix(label)));
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
    /// <param name="maxParallel"></param>
    /// <returns></returns>
    public async Task<ResultSnap> SnapAsync(string vmIdsOrNames,
                                            string label,
                                            int keep,
                                            bool state,
                                            long timeout,
                                            string timestampFormat,
                                            int maxPercentageStorage,
                                            bool onlyRuns,
                                            int maxParallel)
    {
        timestampFormat = GetTimestampFormat(timestampFormat);
        CheckLabelAndTimestampFormat(label, timestampFormat);
        var pveFullVersion = (await client.Version.Version()).ToData().version as string;
        var pveVersion = double.Parse(pveFullVersion!.Split(".")[0]);

        @out.WriteLine($@"ACTION Snap
PVE Version:      {pveFullVersion}
VMs:              {vmIdsOrNames}
Label:            {label}
Keep:             {keep}
State:            {state}
Only running:     {onlyRuns}
Timeout:          {Math.Round(timeout / 1000.0, 1).ToString(CultureInfo.InvariantCulture)} sec.
Timestamp format: {timestampFormat}
Max % Storage :   {maxPercentageStorage}%");

        var snapName = GetSnapName(label, timestampFormat, DateTime.Now);
        var ret = new ResultSnap
        {
            SnapName = snapName
        };
        ret.Start();

        await CallPhaseEventAsync(new(HookPhase.SnapJobStart, null, label, keep, null, state, 0, true));

        var vms = await GetVmsAsync(vmIdsOrNames);
        ret.VmsFound = vms.Any();
        if (!ret.VmsFound) { WriteVmsNotFound(vmIdsOrNames); }

        var nodes = vms.Where(a => !a.IsUnknown).Select(a => a.Node).Distinct().ToList();
        var checkStorage = pveVersion >= 6;
        var storagesCheck = ret.VmsFound
                                ? await CheckStoragesAsync(nodes, maxPercentageStorage, checkStorage, pveFullVersion!)
                                : [];

        var semaphore = new SemaphoreSlim(maxParallel);

        var tasks = vms.Select(async vm =>
        {
            await semaphore.WaitAsync();
            var vmOut = new StringWriter();
            try
            {
                vmOut.WriteLine($"----- VM {vm.VmId} {vm.Type} {vm.Status} -----");

                //node offline: the guest wanted a snapshot and does not get it
                if (vm.IsUnknown)
                {
                    vmOut.WriteLine(SkipUnknown);
                    lock (ret.Vms) { ret.Vms.Add(new ResultSnapVm { VmId = vm.VmId }); }
                    return;
                }

                if (!vm.IsRunning && onlyRuns)
                {
                    vmOut.WriteLine("Skip VM '--only-running' parameter used!");
                    return;
                }

                //exclude template
                if (vm.IsTemplate)
                {
                    vmOut.WriteLine("Skip VM is template");
                    return;
                }

                var vmConfig = await client.GetVmConfigAsync(vm.Node, vm.VmType, vm.VmId);

                //bind mounts and devices: Proxmox VE refuses the snapshot, every time
                var notSnapshottable = GetNotSnapshottableDisks(vmConfig.Disks).Select(a => a.Id).ToList();
                if (notSnapshottable.Count > 0)
                {
                    vmOut.WriteLine($"Skip VM Proxmox VE cannot snapshot it, bind mount or device: {string.Join(", ", notSnapshottable)}");
                    return;
                }

                var resultSnapVm = new ResultSnapVm { VmId = vm.VmId };
                lock (ret.Vms) { ret.Vms.Add(resultSnapVm); }
                resultSnapVm.Start();

                //check agent enabled
                if (vm.VmType == VmType.Qemu && !((VmConfigQemu)vmConfig).AgentEnabled)
                {
                    vmOut.WriteLine($"VM {vm.VmId} consider enabling QEMU agent see https://pve.proxmox.com/wiki/Qemu-guest-agent");
                }

                if (checkStorage && vmConfig.Disks.Any())
                {
                    var validStorage = true;
                    foreach (var item in vmConfig.Disks)
                    {
                        if (storagesCheck.TryGetValue($"{vm.Node}/{item.Storage}", out var isValid) && !isValid)
                        {
                            validStorage = false;
                            break;
                        }
                    }

                    if (!validStorage)
                    {
                        vmOut.WriteLine($"Skip VM problem storage space out of {maxPercentageStorage}%");
                        resultSnapVm.Stop();
                        return;
                    }
                }

                //create snapshot
                await CallPhaseEventAsync(new(HookPhase.SnapCreatePre, vm, label, keep, snapName, state, 0, true) { Out = vmOut });

                vmOut.WriteLine($"Create snapshot: {snapName}");

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
                        inError = await CheckResultAsync(result, vmOut);
                    }
                    catch (Exception ex)
                    {
                        inError = true;
                        LogUnexpectedError(ex);
                        vmOut.WriteLine(ex.Message);
                    }
                }

                if (inError)
                {
                    resultSnapVm.Stop();
                    await CallPhaseEventAsync(new(HookPhase.SnapCreateAbort, vm, label, keep, snapName, state, resultSnapVm.Elapsed.TotalSeconds, false) { Out = vmOut });
                    return;
                }

                //remove old snapshot
                if (!await SnapshotsRemoveAsync(vm, label, keep, timeout, timestampFormat, vmOut))
                {
                    resultSnapVm.Stop();
                    return;
                }

                resultSnapVm.Stop();
                resultSnapVm.Status = true;

                await CallPhaseEventAsync(new(HookPhase.SnapCreatePost, vm, label, keep, snapName, state, resultSnapVm.Elapsed.TotalSeconds, resultSnapVm.Status) { Out = vmOut });

                vmOut.WriteLine($"VM execution {resultSnapVm.Elapsed}");
            }
            finally
            {
                lock (@out) { @out.Write(vmOut); }
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        ret.Stop();

        await CallPhaseEventAsync(new(HookPhase.SnapJobEnd,
                                      null,
                                      label,
                                      keep,
                                      null,
                                      state,
                                      ret.Elapsed.TotalSeconds,
                                      ret.Status));

        @out.WriteLine($"Total execution {ret.Elapsed}");

        LogSnapExit(ret.Status);

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
        CheckLabelAndTimestampFormat(label, timestampFormat);

        @out.WriteLine($@"ACTION Clean
VMs:              {vmIdsOrNames}
Label:            {label}
Keep:             {keep}
Timeout:          {Math.Round(timeout / 1000.0, 1).ToString(CultureInfo.InvariantCulture)} sec.
Timestamp format: {timestampFormat}");

        var watch = new Stopwatch();
        watch.Start();

        var ret = true;
        await CallPhaseEventAsync(new(HookPhase.CleanJobStart, null, label, keep, null, false, 0, false));

        var vms = await GetVmsAsync(vmIdsOrNames);
        if (!vms.Any())
        {
            WriteVmsNotFound(vmIdsOrNames);
            ret = false;
        }

        foreach (var vm in vms)
        {
            @out.WriteLine($"----- VM {vm.VmId} {vm.Type} -----");

            if (vm.IsUnknown)
            {
                @out.WriteLine(SkipUnknown);
                ret = false;
                continue;
            }

            //exclude template
            if (vm.IsTemplate)
            {
                @out.WriteLine("Skip VM is template");
                continue;
            }

            if (!await SnapshotsRemoveAsync(vm, label, keep, timeout, timestampFormat, @out)) { ret = false; }
        }

        watch.Stop();
        await CallPhaseEventAsync(new(HookPhase.CleanJobEnd,
                                      null,
                                      label,
                                      keep,
                                      null,
                                      false,
                                      watch.Elapsed.TotalSeconds,
                                      ret));

        return ret;
    }

    internal async Task<bool> CheckResultAsync(Result result, TextWriter writer)
    {
        //the request was refused: there is no task to check
        if (result.InError() || !result.IsSuccessStatusCode)
        {
            //refused parameters are listed one by one, any other refusal has its reason in the status line
            writer.WriteLine(result.InError() ? result.GetError() : result.ReasonPhrase);
            return true;
        }

        //check error in task
        if (await client.TaskIsRunningAsync(result.ToData<string>()))
        {
            writer.WriteLine("Error task in run... increase the timeout!");
            return true;
        }

        var taskStatus = await client.GetExitStatusTaskAsync(result.ToData<string>());
        if (taskStatus != "OK")
        {
            writer.WriteLine($"Error in task: {taskStatus}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Snapshots of the label beyond the last <paramref name="keep"/>, oldest first.
    /// </summary>
    internal static IEnumerable<VmSnapshot> GetSnapshotsToRemove(IEnumerable<VmSnapshot> snapshots,
                                                                 string label,
                                                                 int keep,
                                                                 string timestampFormat)
        => FilterLabel(snapshots, label, GetTimestampFormat(timestampFormat)).OrderByDescending(a => a.Name)
                                                                             .Skip(keep)
                                                                             .OrderBy(a => a.Name);

    private async Task<bool> SnapshotsRemoveAsync(IClusterResourceVm vm,
                                                  string label,
                                                  int keep,
                                                  long timeout,
                                                  string timestampFormat,
                                                  TextWriter writer)
    {
        foreach (var snapshot in GetSnapshotsToRemove(await SnapshotHelper.GetSnapshotsAsync(client, vm.Node, vm.VmType, vm.VmId),
                                                      label,
                                                      keep,
                                                      timestampFormat))
        {
            var watch = Stopwatch.StartNew();

            await CallPhaseEventAsync(new(HookPhase.SnapRemovePre, vm, label, keep, snapshot.Name, false, 0, false) { Out = writer });

            writer.WriteLine($"Remove snapshot: {snapshot.Name}");

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
                    inError = await CheckResultAsync(result, writer);
                }
                catch (Exception ex)
                {
                    inError = true;
                    LogUnexpectedError(ex);
                    writer.WriteLine(ex.Message);
                }
            }

            watch.Stop();
            if (inError)
            {
                LogSnapRemoveProblem();

                await CallPhaseEventAsync(new(HookPhase.SnapRemoveAbort,
                                              vm,
                                              label,
                                              keep,
                                              snapshot.Name,
                                              false,
                                              watch.Elapsed.TotalSeconds,
                                              false)
                { Out = writer });
                return false;
            }

            await CallPhaseEventAsync(new(HookPhase.SnapRemovePost,
                                          vm,
                                          label,
                                          keep,
                                          snapshot.Name,
                                          false,
                                          watch.Elapsed.TotalSeconds,
                                          true)
            { Out = writer });
        }

        return true;
    }

    /// <summary>
    /// A storage can take snapshots when it is used up to <paramref name="maxPercentageStorage"/> percent;
    /// <paramref name="diskUsagePercentage"/> is a fraction (0.2 = 20%). A storage that reports no size (not
    /// active, or not readable) cannot be checked and does not block the snapshot: Proxmox VE reports the error.
    /// </summary>
    internal static bool IsStorageValid(double diskSize, double diskUsagePercentage, int maxPercentageStorage)
        => diskSize <= 0 || diskUsagePercentage * 100 <= maxPercentageStorage;

    private async Task<Dictionary<string, bool>> CheckStoragesAsync(List<string> nodes,
                                                                    int maxPercentageStorage,
                                                                    bool checkStorage,
                                                                    string pveFullVersion)
    {
        var storagesCheck = new Dictionary<string, bool>();

        if (!checkStorage)
        {
            @out.WriteLine($"The Proxmox VE version {pveFullVersion} does not verify the storage % usage!");
            return storagesCheck;
        }

        var contentAllowed = new[] { "images", "rootdir" };

        var storages = (await client.GetStoragesAsync())
                            .Where(a => !a.IsUnknown && nodes.Contains(a.Node))
                            .ToList();

        if (storages.Exists(a => string.IsNullOrWhiteSpace(a.Content)))
        {
            foreach (var node in nodes)
            {
                var nodeStorages = await client.Nodes[node].Storage.GetAsync(string.Join(",", contentAllowed));
                foreach (var storage in storages.Where(a => a.Node == node))
                {
                    storage.Content = nodeStorages.FirstOrDefault(a => a.Storage == storage.Storage
                                                                        && a.Type == storage.PluginType)
                                                  ?.Content ?? "";
                }
            }
        }

        storages = [.. storages.Where(a => a.Content.Split(',').Any(c => contentAllowed.Contains(c)))
                                .OrderBy(a => a.Node)
                                .ThenBy(a => a.Storage)];

        if (storages.Count == 0) { @out.WriteLine("----- POSSIBLE PROBLEM PERMISSION 'Datastore.Audit' -----"); }

        var storagesPrint = new List<object[]>();
        foreach (var storage in storages)
        {
            var valid = IsStorageValid(storage.DiskSize, storage.DiskUsagePercentage, maxPercentageStorage);

            var key = $"{storage.Node}/{storage.Storage}";
            storagesPrint.Add([key,
                               storage.PluginType,
                               storage.DiskSize <= 0 ? "n/a" : valid ? "Ok" : "Ko",
                               Math.Round(storage.DiskUsagePercentage * 100, 1).ToString(CultureInfo.InvariantCulture),
                               FormatHelper.FromBytes(storage.DiskSize),
                               FormatHelper.FromBytes(storage.DiskUsage)]);

            storagesCheck.Add(key, valid);
        }

        if (storagesPrint.Count != 0)
        {
            var size = new[] { 25, 10, 10, 10, 12, 12 };

            string FormatLine(object[] values)
            {
                var ret = new StringBuilder();
                for (int i = 0; i < size.Length; i++) { ret.Append((values[i] + "").PadLeft(size[i])); }
                return ret.ToString();
            }

            @out.WriteLine(FormatLine(["Storage", "Type", "Valid", "Used % ", "Disk Size", "Disk Usage"]));
            foreach (var item in storagesPrint) { @out.WriteLine(FormatLine(item)); }
        }

        return storagesCheck;
    }
}
