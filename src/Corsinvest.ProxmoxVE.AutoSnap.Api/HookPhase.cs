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
    /// Hook phase
    /// </summary>
    public enum HookPhase
    {
        /// <summary>
        /// Clean Job Start
        /// </summary>
        CleanJobStart = 0,

        /// <summary>
        /// Clean Job End
        /// </summary>
        CleanJobEnd = 1,

        /// <summary>
        /// Snap Job Start
        /// </summary>
        SnapJobStart = 2,

        /// <summary>
        /// Snap Job End
        /// </summary>
        SnapJobEnd = 3,

        /// <summary>
        /// Snap Create Pre
        /// </summary>
        SnapCreatePre = 4,

        /// <summary>
        /// Snap Create Post
        /// </summary>
        SnapCreatePost = 5,

        /// <summary>
        /// Snap Create Abort
        /// </summary>
        SnapCreateAbort = 6,

        /// <summary>
        /// Snap Remove Pre
        /// </summary>
        SnapRemovePre = 7,

        /// <summary>
        /// Snap Remove Post
        /// </summary>
        SnapRemovePost = 8,

        /// <summary>
        /// Snap Remove Abort
        /// </summary>
        SnapRemoveAbort = 9
    }
}