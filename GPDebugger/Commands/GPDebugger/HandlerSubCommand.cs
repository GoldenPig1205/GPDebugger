using CommandSystem;
using System;

namespace GPDebugger.Commands.GPDebugger
{
    internal sealed class HandlerSubCommand : BaseSubCommand, IUsageProvider
    {
        public override string Command => "handler";
        public override string Description => "Manage handler logging, ignore list, and handler list.";
        public string[] Usage => new[] { "<start/stop/list/ignore> [name]" };

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => SubCommandHelper.ExecuteHandler(arguments, sender, out response);
    }
}
