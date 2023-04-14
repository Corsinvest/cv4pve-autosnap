/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: 2019 Copyright Corsinvest Srl
 */

using System.Threading.Tasks;
using Corsinvest.ProxmoxVE.Api.Shell.Helpers;
using Corsinvest.ProxmoxVE.AutoSnap.Api;

namespace Corsinvest.ProxmoxVE.AutoSnap;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rc = ConsoleHelper.CreateApp(Application.Name, "Automatic snapshot VM/CT with retention");
        _ = new Commands(rc);
        return await rc.ExecuteApp(args);
    }
}