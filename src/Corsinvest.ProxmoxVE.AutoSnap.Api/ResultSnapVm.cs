/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

namespace Corsinvest.ProxmoxVE.AutoSnap.Api;

/// <summary>
/// Execution Vm
/// </summary>
public class ResultSnapVm : ResultBaseSnap
{
    /// <summary>
    /// Vm id
    /// </summary>
    /// <value></value>
    public long VmId { get; internal set; }
}