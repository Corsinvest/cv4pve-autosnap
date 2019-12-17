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

namespace Corsinvest.ProxmoxVE.AutoSnap
{
    /// <summary>
    /// Output type
    /// </summary>
    public enum OutputType
    {
        /// <summary>
        /// Unicode
        /// </summary>
        Unicode,

        /// <summary>
        /// Text
        /// </summary>
        Text,

        /// <summary>
        /// Unicode
        /// </summary>
        UnicodeAlt,

        /// <summary>
        /// Markdown
        /// </summary>
        Markdown,

        /// <summary>
        /// Html
        /// </summary>
        Html,

        /// <summary>
        /// Json
        /// </summary>
        Json,

        /// <summary>
        /// Json pretty
        /// </summary>
        JsonPretty,
    }
}