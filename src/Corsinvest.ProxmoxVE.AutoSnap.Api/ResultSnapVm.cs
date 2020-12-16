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

namespace Corsinvest.ProxmoxVE.AutoSnap.Api
{
    /// <summary>
    /// Execution Vm
    /// </summary>
    public class ResultSnapVm : ResultBaseSnap
    {
        /// <summary>
        /// Vm id
        /// </summary>
        /// <value></value>
        public int VmId { get; internal set; }
    }
}