using Exiled.API.Features;
using GPDebugger.Core.Class;
using GPDebugger.Core.Feature;
using System;

namespace GPDebugger
{
    public class Main : Plugin<Config>
    {
        public override string Name => "GPDebugger";
        public override string Author => "GoldenPig1205";
        public override Version Version { get; } = new(1, 0, 5);
        public override Version RequiredExiledVersion { get; } = typeof(Player).Assembly.GetName().Version;

        public static Main Instance { get; set; }

        public override void OnEnabled()
        {
            Instance = this;
            base.OnEnabled();

            DebugManager.EnabledHandlerUsers.Clear();
            DebugManager.EnabledNetworkUsers.Clear();
            DebugManager.KnownHandlers.Clear();
            DebugManager.KnownNetworkMethods.Clear();
            DebugManager.KnownNetworkMessages.Clear();
            DebugManager.HandlerWhitelist.Clear();
            DebugManager.IgnoredHandlers.Clear();
            DebugManager.IgnoredEvents.Clear();
            DebugManager.IgnoredNetworkMethods.Clear();
            DebugManager.IgnoredNetworkMessages.Clear();

            foreach (var handler in Config.HandlerWhitelist)
            {
                if (!string.IsNullOrWhiteSpace(handler))
                    DebugManager.HandlerWhitelist.Add(handler.Trim());
            }

            foreach (var handler in Config.IgnoredHandlers)
            {
                if (!string.IsNullOrWhiteSpace(handler))
                    DebugManager.IgnoredHandlers.Add(handler.Trim());
            }

            foreach (var ignoredEvent in Config.IgnoredEvents)
            {
                if (!string.IsNullOrWhiteSpace(ignoredEvent))
                    DebugManager.IgnoredEvents.Add(ignoredEvent.Trim());
            }

            foreach (var method in Config.IgnoredNetworkMethods)
            {
                if (!string.IsNullOrWhiteSpace(method))
                    DebugManager.IgnoredNetworkMethods.Add(method.Trim());
            }

            foreach (var message in Config.IgnoredNetworkMessages)
            {
                if (!string.IsNullOrWhiteSpace(message))
                    DebugManager.IgnoredNetworkMessages.Add(message.Trim());
            }

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
