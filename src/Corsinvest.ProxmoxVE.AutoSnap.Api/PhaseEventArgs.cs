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

using Corsinvest.ProxmoxVE.Api.Extension.VM;

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
        /// <param name="state"></param>
        public PhaseEventArgs(string phase,
                              VMInfo vm,
                              string label,
                              int keep,
                              string snapName,
                              bool state)
        {
            Phase = phase;
            VM = vm;
            Label = label;
            SnapName = snapName;
            State = state;
        }

        /// <summary>
        /// Phase
        /// </summary>
        /// <value></value>
        public string Phase { get; }

        /// <summary>
        /// VM Info
        /// </summary>
        /// <value></value>
        public VMInfo VM { get; }

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
        public bool State { get; }
    }
}