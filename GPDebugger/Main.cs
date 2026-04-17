using Exiled.API.Features;
using GPDebugger.Core.Class;
using System;

namespace GPDebugger
{
    public class Main : Plugin<Config>
    {
        public override string Name => "GPDebugger";
        public override string Author => "GoldenPig1205";
        public override Version Version { get; } = new(1, 0, 2);
        public override Version RequiredExiledVersion { get; } = typeof(Player).Assembly.GetName().Version;

        public static Main Instance { get; set; }

        public override void OnEnabled()
        {
            Instance = this;
            base.OnEnabled();

            DebugManager.IgnoredEvents.Clear();
            foreach (var ignored in Config.IgnoredEvents)
            {
                DebugManager.IgnoredEvents.Add(ignored);
            }

            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;

            Instance = null;
            base.OnDisabled();
        }

        public void OnWaitingForPlayers()
        {
            DebugManager.EnabledUsers.Clear();
        }
    }
}
