using CommandSystem;
using System;

namespace GPDebugger.Core.Command
{
    internal sealed class StopSubCommand : ICommand
    {
        public string Command => "stop";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Disable debugger for current user.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => GPDebuggerCommand.ExecuteStop(sender, out response);
    }
}
