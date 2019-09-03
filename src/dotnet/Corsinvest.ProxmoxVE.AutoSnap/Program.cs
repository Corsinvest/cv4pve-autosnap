using System;
using Corsinvest.ProxmoxVE.Api.Extension.Utils.Shell;

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