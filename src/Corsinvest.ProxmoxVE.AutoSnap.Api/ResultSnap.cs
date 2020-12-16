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

using System.Collections.Generic;
using System.Linq;

namespace Corsinvest.ProxmoxVE.AutoSnap.Api
{
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
    }
}