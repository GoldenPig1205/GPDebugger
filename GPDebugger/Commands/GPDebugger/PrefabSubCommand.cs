using CommandSystem;
using System;

namespace GPDebugger.Commands.GPDebugger
{
    internal sealed class PrefabSubCommand : BaseSubCommand, IUsageProvider
    {
        public override string Command => "prefab";
        public override string Description => "Spawn, line up, list, or remove debug prefabs.";
        public string[] Usage => new[] { "<spawn/remove/list/lineup> [PrefabType/ID/all/filter/spacing]" };

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            => SubCommandHelper.ExecutePrefab(arguments, sender, out response);
    }
}
