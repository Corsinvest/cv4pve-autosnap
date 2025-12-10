/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Globalization;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;

namespace Corsinvest.ProxmoxVE.AutoSnap.Api;

/// <summary>
/// Phase event arguments
/// </summary>
/// <remarks>
/// Constructor
/// </remarks>
/// <param name="Phase"></param>
/// <param name="Vm"></param>
/// <param name="Label"></param>
/// <param name="Keep"></param>
/// <param name="SnapName"></param>
/// <param name="VmState"></param>
/// <param name="Duration"></param>
/// <param name="Status"></param>
public record PhaseEventArgs(HookPhase Phase,
                            IClusterResourceVm Vm,
                            string Label,
                            int Keep,
                            string SnapName,
                            bool VmState,
                            double Duration,
                            bool Status)
{
    /// <summary>
    /// Environments
    /// </summary>
    /// <value></value>
    public IReadOnlyDictionary<string, string> Environments
        => new Dictionary<string, string>
            {
                {"CV4PVE_AUTOSNAP_PHASE", Application.PhaseEnumToStr(Phase)},
                {"CV4PVE_AUTOSNAP_VMID", Vm.VmId + string.Empty },
                {"CV4PVE_AUTOSNAP_VMNAME", Vm.Name },
                {"CV4PVE_AUTOSNAP_VMTYPE", Vm.Type + string.Empty },
                {"CV4PVE_AUTOSNAP_LABEL", Label },
                {"CV4PVE_AUTOSNAP_KEEP", Keep + string.Empty },
                {"CV4PVE_AUTOSNAP_SNAP_NAME", SnapName },
                {"CV4PVE_AUTOSNAP_VMSTATE", VmState ? "1" : "0" },
                {"CV4PVE_AUTOSNAP_DURATION", (Duration + string.Empty).Replace(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator,".") },
                {"CV4PVE_AUTOSNAP_STATE", Status ? "1" : "0" },
            };
}