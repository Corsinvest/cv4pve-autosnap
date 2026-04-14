/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

namespace Corsinvest.ProxmoxVE.AutoSnap.Api;

/// <summary>
/// Execution Snap
/// </summary>
public class ResultSnap : ResultBaseSnap
{
    /// <summary>
    /// Vms
    /// </summary>
    public List<ResultSnapVm> Vms { get; } = [];

    /// <summary>
    /// Status
    /// </summary>
    public override bool Status => Vms.All(a => a.Status);

    /// <summary>
    /// Name of the snapshot
    /// </summary>
    public string SnapName { get; internal set; } = "";
}