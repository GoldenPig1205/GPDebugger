using Exiled.API.Features;
using System;
using System.Collections.Generic;

namespace GPDebugger.Features
{
    public static class DebugManager
    {
        public static HashSet<string> EnabledHandlerUsers = new();
        public static HashSet<string> EnabledNetworkUsers = new();

        public static HashSet<string> HandlerWhitelist = new(StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> KnownHandlers = new(StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> IgnoredHandlers = new(StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> IgnoredEvents = new(StringComparer.OrdinalIgnoreCase);

        public static HashSet<string> KnownNetworkMethods = new(StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> KnownNetworkMessages = new(StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> IgnoredNetworkMethods = new(StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> IgnoredNetworkMessages = new(StringComparer.OrdinalIgnoreCase);

        public static bool IsEnabled(Player player) => EnabledHandlerUsers.Contains(player.UserId) || EnabledNetworkUsers.Contains(player.UserId);

        public static bool IsHandlerEnabled(string handler)
        {
            if (HandlerWhitelist.Count > 0 && !HandlerWhitelist.Contains(handler))
                return false;

            return !IgnoredHandlers.Contains(handler);
        }

        public static void LoadConfigLists(Configs.Config config)
        {
            EnabledHandlerUsers.Clear();
            EnabledNetworkUsers.Clear();
            KnownHandlers.Clear();
            KnownNetworkMethods.Clear();
            KnownNetworkMessages.Clear();
            HandlerWhitelist.Clear();
            IgnoredHandlers.Clear();
            IgnoredEvents.Clear();
            IgnoredNetworkMethods.Clear();
            IgnoredNetworkMessages.Clear();

            foreach (var handler in config.HandlerWhitelist)
            {
                if (!string.IsNullOrWhiteSpace(handler))
                    HandlerWhitelist.Add(handler.Trim());
            }

            foreach (var handler in config.IgnoredHandlers)
            {
                if (!string.IsNullOrWhiteSpace(handler))
                    IgnoredHandlers.Add(handler.Trim());
            }

            foreach (var ignoredEvent in config.IgnoredEvents)
            {
                if (!string.IsNullOrWhiteSpace(ignoredEvent))
                    IgnoredEvents.Add(ignoredEvent.Trim());
            }

            foreach (var method in config.IgnoredNetworkMethods)
            {
                if (!string.IsNullOrWhiteSpace(method))
                    IgnoredNetworkMethods.Add(method.Trim());
            }

            foreach (var message in config.IgnoredNetworkMessages)
            {
                if (!string.IsNullOrWhiteSpace(message))
                    IgnoredNetworkMessages.Add(message.Trim());
            }
        }
    }
}
