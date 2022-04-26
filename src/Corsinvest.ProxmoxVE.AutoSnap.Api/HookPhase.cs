/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: 2020 Copyright Corsinvest Srl
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