using CommandSystem;
using System;

namespace GPDebugger.Core.Command
{
    internal sealed class StartSubCommand : ICommand
    {
        public string Command => "start";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Enable debugger and subscribe events.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => GPDebuggerCommand.ExecuteStart(sender, out response);
    }
}
