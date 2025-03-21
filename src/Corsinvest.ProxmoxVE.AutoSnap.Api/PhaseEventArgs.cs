/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: 2019 Copyright Corsinvest Srl
 */

using System.Globalization;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;

namespace Corsinvest.ProxmoxVE.AutoSnap.Api
{
    /// <summary>
    /// Phase event arguments
    /// </summary>
    public class PhaseEventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="phase"></param>
        /// <param name="vm"></param>
        /// <param name="label"></param>
        /// <param name="keep"></param>
        /// <param name="snapName"></param>
        /// <param name="vmState"></param>
        /// <param name="duration"></param>
        /// <param name="status"></param>
        public PhaseEventArgs(HookPhase phase,
                              IClusterResourceVm vm,
                              string label,
                              int keep,
                              string snapName,
                              bool vmState,
                              double duration,
                              bool status)
        {
            Phase = phase;
            Vm = vm;
            Label = label;
            Keep = keep;
            SnapName = snapName;
            VMState = vmState;
            Duration = duration;
            Status = status;
        }

        /// <summary>
        /// Phase
        /// </summary>
        /// <value></value>
        public HookPhase Phase { get; }

        /// <summary>
        /// VM Info
        /// </summary>
        /// <value></value>
        public IClusterResourceVm Vm { get; }

        /// <summary>
        /// Label
        /// </summary>
        /// <value></value>
        public string Label { get; }

        /// <summary>
        /// Keep
        /// </summary>
        /// <value></value>
        public int Keep { get; }

        /// <summary>
        /// Snapshot name
        /// </summary>
        /// <value></value>
        public string SnapName { get; }

        /// <summary>
        /// State memory
        /// </summary>
        /// <value></value>
        public bool VMState { get; }

        /// <summary>
        /// Duration
        /// </summary>
        /// <value></value>
        public double Duration { get; }

        /// <summary>
        /// Status
        /// </summary>
        /// <value></value>
        public bool Status { get; }

        /// <summary>
        /// Environments
        /// </summary>
        /// <value></value>
        public IReadOnlyDictionary<string, string> Environments
            => new Dictionary<string, string>
                {
                    {"CV4PVE_AUTOSNAP_PHASE", Application.PhaseEnumToStr(Phase)},
                    {"CV4PVE_AUTOSNAP_VMID", Vm.VmId + "" },
                    {"CV4PVE_AUTOSNAP_VMNAME", Vm.Name },
                    {"CV4PVE_AUTOSNAP_VMTYPE", Vm.Type + "" },
                    {"CV4PVE_AUTOSNAP_LABEL", Label },
                    {"CV4PVE_AUTOSNAP_KEEP", Keep + "" },
                    {"CV4PVE_AUTOSNAP_SNAP_NAME", SnapName },
                    {"CV4PVE_AUTOSNAP_VMSTATE", VMState ? "1" : "0" },
                    {"CV4PVE_AUTOSNAP_DURATION", (Duration + "").Replace(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator,".") },
                    {"CV4PVE_AUTOSNAP_STATE", Status ? "1" : "0" },
                };
    }
}