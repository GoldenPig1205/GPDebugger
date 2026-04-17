using CommandSystem;
using Exiled.API.Features;
using GPDebugger.Core.Class;
using System;
using GPDebugger.Core.Feature;
using System.Linq;

namespace GPDebugger.Core.Command
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class GPDebuggerCommand : ICommand, IUsageProvider
    {
        public string Command => "gpdebugger";
        public string[] Aliases => new string[] { "gpdebug" };
        public string Description => "Debug tool";
        public string[] Usage => new string[] { "start/stop/handler/ignore/print/list" };

        private static System.Collections.Generic.List<System.Reflection.MethodInfo> _cachedGetMethods;

        private static void EnsureCacheInit()
        {
            if (_cachedGetMethods != null) return;
            _cachedGetMethods = new System.Collections.Generic.List<System.Reflection.MethodInfo>();

            var types = typeof(Server).Assembly.GetTypes()
                .Where(t => t.IsClass && !string.IsNullOrEmpty(t.Namespace) && t.Namespace.StartsWith("Exiled.API.Features"));

            foreach (var type in types)
            {
                foreach (var method in type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                {
                    if (method.Name == "Get" && method.GetParameters().Length == 1)
                    {
                        var paramType = method.GetParameters()[0].ParameterType;
                        if (paramType == typeof(UnityEngine.GameObject) || 
                            paramType == typeof(UnityEngine.Transform) || 
                            paramType == typeof(UnityEngine.Collider))
                        {
                            _cachedGetMethods.Add(method);
                        }
                    }
                }
            }
        }

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);

            if (arguments.Count == 0)
            {
                response = "Usage:\n- GPDebugger start\n- GPDebugger stop\n- GPDebugger handler <add/remove> <Player/Server/etc>\n- GPDebugger ignore <add/remove> <EventName>\n- GPDebugger print <class/player/hit> [playerName]\n- GPDebugger list";
                return false;
            }

            switch (arguments.At(0))
            {
                case "list":
                    var classNames = typeof(Server).Assembly.GetTypes()
                        .Where(t => t.IsClass && t.Namespace == "Exiled.API.Features" && t.IsAbstract && t.IsSealed && !t.Name.Contains("<"))
                        .Select(t => t.Name)
                        .OrderBy(n => n);
                    response = "Available print classes:\n" + string.Join(", ", classNames);
                    return true;

                case "start":
                    DebugManager.EnabledUsers.Add(player.UserId);
                    HandlerLog.RegisterAllEvents();
                    response = "Debug ON + All events subscribed";
                    return true;

                case "stop":
                    DebugManager.EnabledUsers.Remove(player.UserId);
                    response = "Debug OFF";
                    return true;

                case "handler":
                    if (arguments.Count < 3)
                    {
                        response = "Usage: GPDebugger handler <add/remove> <EventClass>\nExample: GPDebugger handler add Player\nExample: GPDebugger handler remove Server";
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
                        response = "Usage: GPDebugger ignore <add/remove> <EventName>\nExample: GPDebugger ignore add Player.MakingNoiseEventArgs";
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

                case "print":
                    if (arguments.Count < 2)
                    {
                        response = "Usage: GPDebugger print <class/player/hit> [player]\n- class: Print public static properties of an Exiled.API.Features class (e.g. Server, Map)\n- player: Print player properties (yourself or [player] if provided)\n- hit: Print properties of the object you are looking at (Raycast)";
                        return false;
                    }

                    string targetTypeInfo = arguments.At(1).ToLower();

                    if (targetTypeInfo == "hit")
                    {
                        var startPos = player.CameraTransform.position + player.CameraTransform.forward * 0.2f;
                        if (UnityEngine.Physics.Raycast(startPos, player.CameraTransform.forward, out UnityEngine.RaycastHit hit, 100f))
                        {
                            EnsureCacheInit();
                            var targetGo = hit.collider.gameObject;
                            var foundObjects = new System.Collections.Generic.HashSet<object>();
                            var currentTransform = targetGo.transform;

                            while (currentTransform != null && foundObjects.Count == 0)
                            {
                                foreach (var method in _cachedGetMethods)
                                {
                                    try
                                    {
                                        var paramType = method.GetParameters()[0].ParameterType;
                                        object arg = null;

                                        if (paramType == typeof(UnityEngine.GameObject)) arg = currentTransform.gameObject;
                                        else if (paramType == typeof(UnityEngine.Transform)) arg = currentTransform;
                                        else if (paramType == typeof(UnityEngine.Collider) && currentTransform == targetGo.transform) arg = hit.collider;

                                        if (arg != null)
                                        {
                                            var result = method.Invoke(null, new[] { arg });
                                            if (result != null)
                                            {
                                                foundObjects.Add(result);
                                            }
                                        }
                                    }
                                    catch { }
                                }

                                if (foundObjects.Count > 0) break;
                                currentTransform = currentTransform.parent;
                            }

                            if (foundObjects.Count > 0)
                            {
                                var sb = new System.Text.StringBuilder();
                                foreach (var obj in foundObjects)
                                {
                                    sb.AppendLine(PrintProperties(obj.GetType(), obj, $"--- {obj.GetType().Name} Info ---"));
                                }
                                response = sb.ToString();
                                player?.SendConsoleMessage(response, "white");
                                return true;
                            }
                            else
                            {
                                response = PrintProperties(typeof(UnityEngine.GameObject), targetGo, $"--- GameObject Info: <color=#55aaff>{targetGo.name}</color> ---");
                                player?.SendConsoleMessage(response, "white");
                                return true;
                            }
                        }
                        else
                        {
                            response = "You are not looking at anything.";
                            return false;
                        }
                    }
                    else if (targetTypeInfo == "player")
                    {
                        Player targetPlayer = player;
                        if (arguments.Count >= 3)
                        {
                            targetPlayer = Player.Get(arguments.At(2));
                            if (targetPlayer == null)
                            {
                                response = $"Player not found: {arguments.At(2)}";
                                return false;
                            }
                        }

                        response = PrintProperties(typeof(Player), targetPlayer, $"--- Player Info: <color=#55aaff>{targetPlayer.Nickname}</color> ---");
                        player?.SendConsoleMessage(response, "white");
                        return true;
                    }
                    else
                    {
                        var targetType = typeof(Server).Assembly.GetTypes()
                            .FirstOrDefault(t => t.IsClass && t.Namespace == "Exiled.API.Features" && t.IsAbstract && t.IsSealed && t.Name.Equals(targetTypeInfo, StringComparison.OrdinalIgnoreCase));

                        if (targetType != null)
                        {
                            response = PrintProperties(targetType, null, $"--- {targetType.Name} Info ---");
                            player?.SendConsoleMessage(response, "white");
                            return true;
                        }

                        response = $"Target '{targetTypeInfo}' not found in Exiled.API.Features or is not a public static class.";
                        return false;
                    }
            }

            response = "Invalid command";
            return false;
        }

        public static string FormatValue(object val)
        {
            string valStr;
            if (val is bool b)
            {
                valStr = b ? "<color=green>True</color>" : "<color=red>False</color>";
            }
            else if (val is System.Collections.IEnumerable enumerable && !(val is string))
            {
                var items = new System.Collections.Generic.List<string>();
                foreach (var item in enumerable)
                {
                    items.Add(item?.ToString() ?? "null");
                }
                valStr = "[" + string.Join(", ", items) + "]";
            }
            else
            {
                valStr = val?.ToString() ?? "null";
                if (val != null && (valStr == val.GetType().ToString() || val.GetType().IsValueType && !val.GetType().IsPrimitive && !val.GetType().IsEnum))
                {
                    var subItems = new System.Collections.Generic.List<string>();
                    foreach (var p in val.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    {
                        if (p.GetIndexParameters().Length > 0) continue;
                        try { subItems.Add($"{p.Name}: {p.GetValue(val)}"); } catch { }
                    }
                    foreach (var f in val.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    {
                        try { subItems.Add($"{f.Name}: {f.GetValue(val)}"); } catch { }
                    }
                    if (subItems.Count > 0)
                        valStr = "{" + string.Join(", ", subItems) + "}";
                }
            }
            return valStr;
        }

        public static string PrintProperties(Type type, object instance, string header)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(header);

            var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
            if (instance != null) flags |= System.Reflection.BindingFlags.Instance;

            foreach (var prop in type.GetProperties(flags))
            {
                if (prop.GetIndexParameters().Length > 0) continue;
                try
                {
                    object val = prop.GetValue((prop.GetMethod?.IsStatic ?? false) ? null : instance);
                    string valStr = FormatValue(val);
                    sb.AppendLine($"<b>{prop.Name}</b>: {valStr}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"<b>{prop.Name}</b>: [Error] {ex.Message}");
                }
            }

            return "\n" + string.Join("\n", sb.ToString()
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => $"<size=15>{line}</size>"));
        }
    }
}
