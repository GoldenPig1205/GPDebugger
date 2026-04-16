using CommandSystem;
using Exiled.API.Features;
using GPDebugger.Core.Class;
using LabApi.Loader.Features.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPDebugger.Core.Command
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class GPDebuggerCommand : ICommand, IUsageProvider
    {
        public string Command => "GPDebugger";
        public string[] Aliases => new string[] {};
        public string Description => "Debug tool";
        public string[] Usage => new string[] { "start/stop/handler/ignore" };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);

            if (arguments.Count == 0)
            {
                response = "Usage: GPDebugger start/stop/handler/ignore";
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
                        response = "Usage: GPDebugger handler add/remove Player";
                        return false;
                    }

                    string action = arguments.At(1);
                    string handler = arguments.At(2);

                    if (action == "add")
                    {
                        if (DebugManager.EnabledHandlers.Add(handler))
                        {
                            response = $"Handler {handler} added";
                            return true;
                        }

                        response = $"Handler {handler} is already added.";
                        return false;
                    }

                    if (action == "remove")
                    {
                        if (DebugManager.EnabledHandlers.Remove(handler))
                        {
                            response = $"Handler {handler} removed";
                            return true;
                        }

                        response = $"Handler {handler} is not in the list.";
                        return false;
                    }

                    break;

                case "ignore":
                    if (arguments.Count < 3)
                    {
                        response = "Usage: GPDebugger ignore add/remove Player.MakingNoiseEventArgs";
                        return false;
                    }

                    string ignoreAction = arguments.At(1);
                    string eventName = arguments.At(2);

                    if (ignoreAction == "add")
                    {
                        if (DebugManager.IgnoredEvents.Add(eventName))
                        {
                            response = $"Event {eventName} is now ignored.";
                            return true;
                        }

                        response = $"Event {eventName} is already ignored.";
                        return false;
                    }

                    if (ignoreAction == "remove")
                    {
                        if (DebugManager.IgnoredEvents.Remove(eventName))
                        {
                            response = $"Event {eventName} removed from ignore list.";
                            return true;
                        }

                        response = $"Event {eventName} is not in the ignore list.";
                        return false;
                    }

                    break;
            }

            response = "Invalid command";
            return false;
        }
    }
}
