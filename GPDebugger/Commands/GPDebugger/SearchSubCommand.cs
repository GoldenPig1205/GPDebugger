using CommandSystem;
using System;

namespace GPDebugger.Commands.GPDebugger
{
    internal sealed class SearchSubCommand : BaseSubCommand, IUsageProvider
    {
        public override string Command => "search";
        public override string[] Aliases => new[] { "find", "transform", "tf" };
        public override string Description => "Search scene transforms by name or by component type.";
        public string[] Usage => new[] { "<name> [number]", "component <componentName> [number]" };

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => SubCommandHelper.ExecuteSearch(arguments, sender, out response);
    }
}
