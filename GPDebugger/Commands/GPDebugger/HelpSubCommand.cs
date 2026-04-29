using CommandSystem;
using System;

namespace GPDebugger.Commands.GPDebugger
{
    internal sealed class HelpSubCommand : BaseSubCommand
    {
        public override string Command => "help";
        public override string Description => "Show detailed help for gpdebug.";

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = SubCommandHelper.BuildHelpMessage();
            return false;
        }
    }
}
