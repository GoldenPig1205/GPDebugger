using CommandSystem;
using System;

namespace GPDebugger.Core.Command
{
    internal sealed class PrintSubCommand : ICommand, IUsageProvider
    {
        public string Command => "print";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Print debug info.";
        public string[] Usage => new[] { "<class/player/hit> [playerName]" };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => GPDebuggerCommand.ExecutePrint(arguments, sender, out response);
    }
}
