using CommandSystem;
using System;

namespace GPDebugger.Commands.GPDebugger
{
    internal sealed class ListSubCommand : BaseSubCommand
    {
        public override string Command => "list";
        public override string Description => "Compatibility alias for handler list.";

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => GPDebuggerActions.ExecuteList(out response);
    }
}
