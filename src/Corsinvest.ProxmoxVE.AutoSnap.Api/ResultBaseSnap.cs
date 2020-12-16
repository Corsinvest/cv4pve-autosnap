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

using System;
using System.Diagnostics;

namespace Corsinvest.ProxmoxVE.AutoSnap.Api
{
    /// <summary>
    /// ResultBase
    /// </summary>
    public class ResultBaseSnap
    {
        private readonly Stopwatch _execution = new Stopwatch();

        internal void Start() => _execution.Start();
        internal void Stop() => _execution.Stop();

        /// <summary>
        /// Elapsed
        /// </summary>
        public TimeSpan Elapsed => _execution.Elapsed;

        /// <summary>
        /// Status
        /// </summary>
        /// <value></value>
        public virtual bool Status { get; internal set; }
    }
}