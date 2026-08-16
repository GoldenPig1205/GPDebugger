using CommandSystem;
using GPDebugger.Commands.GPDebugger;
using System;

namespace GPDebugger.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class GPDebuggerCommand : ParentCommand, IUsageProvider
    {
        public GPDebuggerCommand() => LoadGeneratedCommands();

        public override string Command => "gpdebug";
        public override string[] Aliases => new[] { "gpdebugger", "ggdebug" };
        public override string Description => "Debug tool";
        public string[] Usage => new[] { "help/handler/network/pointer/prefab/time/print/search" };

        public override void LoadGeneratedCommands()
        {
            RegisterCommand(new HelpSubCommand());
            RegisterCommand(new ListSubCommand());
            RegisterCommand(new HandlerSubCommand());
            RegisterCommand(new NetworkSubCommand());
            RegisterCommand(new PointerSubCommand());
            RegisterCommand(new PrefabSubCommand());
            RegisterCommand(new TimeSubCommand());
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
