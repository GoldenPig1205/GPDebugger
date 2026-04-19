using CommandSystem;
using System;

namespace GPDebugger.Commands.GPDebugger
{
    internal sealed class NetworkSubCommand : ICommand, IUsageProvider
    {
        public string Command => "network";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Manage network logging, ignore list, and network list.";
        public string[] Usage => new[] { "<start/stop/list/ignore> [name]" };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => GPDebuggerCommand.ExecuteNetwork(arguments, sender, out response);
    }
}
