using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPDebug.Core.Class
{
    public static class DebugManager
    {
        public static HashSet<string> EnabledUsers = new();
        public static HashSet<string> EnabledHandlers = new(StringComparer.OrdinalIgnoreCase);
        public static HashSet<string> IgnoredEvents = new(StringComparer.OrdinalIgnoreCase);

        public static bool IsEnabled(Player player) => EnabledUsers.Contains(player.UserId);

        public static bool IsHandlerEnabled(string handler)
        {
            if (EnabledHandlers.Count == 0)
                return true;

            return EnabledHandlers.Contains(handler);
        }
    }
}
