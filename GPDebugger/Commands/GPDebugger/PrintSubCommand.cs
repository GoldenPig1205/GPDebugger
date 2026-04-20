using CommandSystem;
using System;

namespace GPDebugger.Commands.GPDebugger
{
    internal sealed class PrintSubCommand : BaseSubCommand, IUsageProvider
    {
        public override string Command => "print";
        public override string Description => "Print debug info.";
        public string[] Usage => new[] { "<class/player/hit> [playerName]" };

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => GPDebuggerActions.ExecutePrint(arguments, sender, out response);
    }
}
