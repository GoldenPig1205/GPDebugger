using CommandSystem;
using System;

namespace GPDebugger.Commands.GPDebugger
{
    internal abstract class BaseSubCommand : ICommand
    {
        public abstract string Command { get; }
        public virtual string[] Aliases => Array.Empty<string>();
        public abstract string Description { get; }
        public abstract bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response);
    }
}
