using CommandSystem;
using System;

namespace GPDebugger.Commands.GPDebugger
{
    internal sealed class HandlerSubCommand : ICommand, IUsageProvider
    {
        public string Command => "handler";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Manage handler logging, ignore list, and handler list.";
        public string[] Usage => new[] { "<start/stop/list/ignore> [name]" };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => GPDebuggerCommand.ExecuteHandler(arguments, sender, out response);
    }
}
