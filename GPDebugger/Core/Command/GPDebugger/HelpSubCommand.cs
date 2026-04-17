using CommandSystem;
using System;

namespace GPDebugger.Core.Command
{
    internal sealed class HelpSubCommand : ICommand
    {
        public string Command => "help";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Show detailed help for gpdebug.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => GPDebuggerCommand.ExecuteHelp(out response);
    }
}
