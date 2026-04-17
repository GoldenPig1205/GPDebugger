using CommandSystem;
using System;

namespace GPDebugger.Core.Command
{
    internal sealed class HandlerSubCommand : ICommand, IUsageProvider
    {
        public string Command => "handler";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Manage enabled handlers.";
        public string[] Usage => new[] { "<add/remove> <EventClass>" };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => GPDebuggerCommand.ExecuteHandler(arguments, out response);
    }
}
