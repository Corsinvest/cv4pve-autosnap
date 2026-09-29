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
    /// The selection found at least one VM/CT
    /// </summary>
    public bool VmsFound { get; internal set; } = true;

    /// <summary>
    /// Status: false when no VM/CT was found or one failed; skipped templates and stopped VM/CT do not count
    /// </summary>
    public override bool Status => VmsFound && Vms.All(a => a.Status);

    /// <summary>
    /// Name of the snapshot
    /// </summary>
    public string SnapName { get; internal set; } = "";
}
