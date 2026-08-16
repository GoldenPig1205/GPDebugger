using CommandSystem;
using System;

namespace GPDebugger.Commands.GPDebugger
{
    internal sealed class TimeSubCommand : BaseSubCommand, IUsageProvider
    {
        public override string Command => "time";
        public override string[] Aliases => new[] { "timescale", "clock" };
        public override string Description => "Pause, resume, or scale the server simulation time.";
        public string[] Usage => new[] { "<pause/freeze/unfreeze/resume/scale/status> [0-10]" };

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => SubCommandHelper.ExecuteTime(arguments, sender, out response);
    }
}
