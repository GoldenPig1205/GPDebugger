using CommandSystem;
using Exiled.API.Features;
using GPDebug.Core.Class;
using LabApi.Loader.Features.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPDebug.Core.Command
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class GpDebugCommand : ICommand, IUsageProvider
    {
        public string Command => "gpdebug";
        public string[] Aliases => new string[] {};
        public string Description => "Debug tool";
        public string[] Usage => new string[] { "start/stop/handler" };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);

            if (arguments.Count == 0)
            {
                response = "Usage: gpdebug start/stop/handler";
                return false;
            }

            switch (arguments.At(0))
            {
                case "start":
                    DebugManager.EnabledUsers.Add(player.UserId);
                    Main.Instance.RegisterAllEvents();
                    response = "Debug ON + All events subscribed";
                    return true;

                case "stop":
                    DebugManager.EnabledUsers.Remove(player.UserId);
                    response = "Debug OFF";
                    return true;

                case "handler":
                    if (arguments.Count < 3)
                    {
                        response = "Usage: gpdebug handler add/remove Player";
                        return false;
                    }

                    string action = arguments.At(1);
                    string handler = arguments.At(2);

                    if (action == "add")
                    {
                        DebugManager.EnabledHandlers.Add(handler);
                        response = $"Handler {handler} added";
                        return true;
                    }

                    if (action == "remove")
                    {
                        DebugManager.EnabledHandlers.Remove(handler);
                        response = $"Handler {handler} removed";
                        return true;
                    }

                    break;
            }

            response = "Invalid command";
            return false;
        }
    }
}
