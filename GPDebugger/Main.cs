using Exiled.API.Features;
using GPDebugger.Features;
using GPDebugger.Configs;
using System;

namespace GPDebugger
{
    public class Main : Plugin<Config>
    {
        public override string Name => "GPDebugger";
        public override string Author => "GoldenPig1205";
        public override Version Version { get; } = new(1, 0, 6);
        public override Version RequiredExiledVersion { get; } = new Version(9, 13, 2);

        public static Main Instance { get; set; }

        public override void OnEnabled()
        {
            Instance = this;
            base.OnEnabled();

            DebugManager.LoadConfigLists(Config);

            HandlerLog.RegisterAllEvents();
            NetworkLog.RegisterAllEvents();

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
            DebugManager.EnabledHandlerUsers.Clear();
            DebugManager.EnabledNetworkUsers.Clear();
        }
    }
}
