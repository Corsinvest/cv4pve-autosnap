/*
 * This file is part of the cv4pve-autosnap https://github.com/Corsinvest/cv4pve-autosnap,
 * Copyright (C) 2016 Corsinvest Srl
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using Corsinvest.ProxmoxVE.Api.Extension.Helpers.Shell;

namespace Corsinvest.ProxmoxVE.AutoSnap
{
    class Program
    {
        public static int Main(string[] args)
        {
            var app = ShellHelper.CreateConsoleApp(Commands.APPLICATION_NAME, "Automatic snapshot VM/CT with retention");
            new ShellCommands(app);
            return app.ExecuteConsoleApp(Console.Out, args);
        }
    }
}