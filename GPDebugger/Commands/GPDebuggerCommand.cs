using CommandSystem;
using GPDebugger.Commands.GPDebugger;
using System;

namespace GPDebugger.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class GPDebuggerCommand : ParentCommand, IUsageProvider
    {
        public GPDebuggerCommand() => LoadGeneratedCommands();

        public override string Command => "gpdebugger";
        public override string[] Aliases => new[] { "gpdebug" };
        public override string Description => "Debug tool";
        public string[] Usage => new[] { "help/handler/network/print/search" };

        public override void LoadGeneratedCommands()
        {
            RegisterCommand(new HelpSubCommand());
            RegisterCommand(new ListSubCommand());
            RegisterCommand(new HandlerSubCommand());
            RegisterCommand(new NetworkSubCommand());
            RegisterCommand(new PrintSubCommand());
            RegisterCommand(new SearchSubCommand());
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = SubCommandHelper.BuildHelpMessage();
            return false;
        }
    }
}
