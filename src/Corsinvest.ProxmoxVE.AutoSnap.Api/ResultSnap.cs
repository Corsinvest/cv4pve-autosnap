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
    public List<ResultSnapVm> Vms { get; } = new List<ResultSnapVm>();

    /// <summary>
    /// Status
    /// </summary>
    /// <value></value>
    public override bool Status
    {
        get => !Vms.Any(a => !a.Status);
        internal set { }
    }

    /// <summary>
    /// Name of the snapshot
    /// </summary>
    public string SnapName { get; internal set; } = default!;
}