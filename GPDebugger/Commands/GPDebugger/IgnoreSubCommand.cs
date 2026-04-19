using CommandSystem;
using System;

namespace GPDebugger.Commands.GPDebugger
{
    internal sealed class IgnoreSubCommand : ICommand, IUsageProvider
    {
        public string Command => "ignore";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Manage ignored events.";
        public string[] Usage => new[] { "<add/remove> <EventName>" };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => GPDebuggerCommand.ExecuteIgnore(arguments, out response);
    }
}
