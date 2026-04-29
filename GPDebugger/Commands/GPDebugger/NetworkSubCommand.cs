using CommandSystem;
using System;

namespace GPDebugger.Commands.GPDebugger
{
    internal sealed class NetworkSubCommand : BaseSubCommand, IUsageProvider
    {
        public override string Command => "network";
        public override string Description => "Manage network logging, ignore list, and network list.";
        public string[] Usage => new[] { "<start/stop/list/ignore> [name]" };

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => SubCommandHelper.ExecuteNetwork(arguments, sender, out response);
    }
}
