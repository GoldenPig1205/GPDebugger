using CommandSystem;
using System;

namespace GPDebugger.Core.Command
{
    internal sealed class ListSubCommand : ICommand
    {
        public string Command => "list";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "List available print classes.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => GPDebuggerCommand.ExecuteList(out response);
    }
}
