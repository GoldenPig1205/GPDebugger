using CommandSystem;
using System;

namespace GPDebugger.Commands.GPDebugger
{
    internal sealed class IgnoreSubCommand : BaseSubCommand, IUsageProvider
    {
        public override string Command => "ignore";
        public override string Description => "Manage ignored events.";
        public string[] Usage => new[] { "<add/remove> <EventName>" };

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => GPDebuggerActions.ExecuteIgnore(arguments, out response);
    }
}
