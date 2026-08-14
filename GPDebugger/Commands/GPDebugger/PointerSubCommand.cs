using CommandSystem;
using System;

namespace GPDebugger.Commands.GPDebugger
{
    internal sealed class PointerSubCommand : BaseSubCommand, IUsageProvider
    {
        public override string Command => "pointer";
        public override string[] Aliases => new[] { "look", "watch" };
        public override string Description => "Toggle the live pointer Transform inspector.";
        public string[] Usage => new[] { "<on/off>" };

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => SubCommandHelper.ExecutePointer(arguments, sender, out response);
    }
}
