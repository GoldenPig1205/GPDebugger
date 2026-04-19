using CommandSystem;
using System;

namespace GPDebugger.Commands.GPDebugger
{
    internal sealed class ListSubCommand : ICommand
    {
        public string Command => "list";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Compatibility alias for handler list.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => GPDebuggerCommand.ExecuteHandlerList(out response);
    }
}
