using CommandSystem;
using Exiled.API.Features;
using GPDebugger.Features;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace GPDebugger.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class GPDebuggerCommand : ParentCommand, IUsageProvider
    {
        public GPDebuggerCommand() => LoadGeneratedCommands();

        public override string Command => "gpdebugger";
        public override string[] Aliases => new[] { "gpdebug" };
        public override string Description => "Debug tool";
        public string[] Usage => new[] { "help/handler/network/print" };

        private static System.Collections.Generic.List<System.Reflection.MethodInfo> _cachedGetMethods;

        public override void LoadGeneratedCommands()
        {
            RegisterCommand(new GPDebugger.HelpSubCommand());
            RegisterCommand(new GPDebugger.ListSubCommand());
            RegisterCommand(new GPDebugger.HandlerSubCommand());
            RegisterCommand(new GPDebugger.NetworkSubCommand());
            RegisterCommand(new GPDebugger.PrintSubCommand());
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = BuildHelpMessage();
            return false;
        }

        internal static bool ExecuteHelp(out string response)
        {
            response = BuildHelpMessage();
            return false;
        }

        internal static string BuildHelpMessage()
        {
            return
                "GPDebugger Commands:\n" +
                "- <color=white>gpdebug help</color>\n" +
                "  Shows this help message.\n" +
                "- <color=white>gpdebug handler start</color>\n" +
                "  Enables event handler logging for you.\n" +
                "- <color=white>gpdebug handler stop</color>\n" +
                "  Disables event handler logging for you.\n" +
                "- <color=white>gpdebug handler ignore add <HandlerName></color>\n" +
                "  Ignores a handler from event logging.\n" +
                "- <color=white>gpdebug handler ignore remove <HandlerName></color>\n" +
                "  Removes a handler from the ignore list.\n" +
                "- <color=white>gpdebug handler list</color>\n" +
                "  Shows ignored handlers, active handlers, and available Exiled.API.Features classes.\n" +
                "- <color=white>gpdebug network start</color>\n" +
                "  Enables network method/message logging for you.\n" +
                "- <color=white>gpdebug network stop</color>\n" +
                "  Disables network method/message logging for you.\n" +
                "- <color=white>gpdebug network ignore add <Name></color>\n" +
                "  Ignores a network method or message by name.\n" +
                "- <color=white>gpdebug network ignore remove <Name></color>\n" +
                "  Removes a network method or message from the ignore list.\n" +
                "- <color=white>gpdebug network list</color>\n" +
                "  Shows ignored and active network methods/messages.\n" +
                "- <color=white>gpdebug print <class/player/hit> [playerName] [componentName]</color>\n" +
                "  class: Prints public static properties of an Exiled.API.Features class (e.g. Server, Map).\n" +
                "  player: Prints player properties (self or target player). Optionally specify [componentName] to inspect a component.\n" +
                "  hit: Prints object info you are looking at. Optionally specify [componentName] to inspect a component.\n" +
                "  Examples: gpdebug print player, gpdebug print player 8 CharacterController, gpdebug print hit Rigidbody\n";
        }

        internal static bool ExecuteList(out string response)
        {
            return ExecuteHandlerList(out response);
        }

        internal static bool ExecuteHandlerList(out string response)
        {
            var whitelist = DebugManager.HandlerWhitelist.OrderBy(x => x).ToArray();
            var ignored = DebugManager.IgnoredHandlers.OrderBy(x => x).ToArray();
            var active = DebugManager.KnownHandlers
                .Where(x => (DebugManager.HandlerWhitelist.Count == 0 || DebugManager.HandlerWhitelist.Contains(x)) && !DebugManager.IgnoredHandlers.Contains(x))
                .OrderBy(x => x)
                .ToArray();
            var classes = typeof(Server).Assembly.GetTypes()
                .Where(t => t.IsClass && t.Namespace == "Exiled.API.Features" && t.IsAbstract && t.IsSealed && !t.Name.Contains("<"))
                .Select(t => t.Name)
                .OrderBy(n => n)
                .ToArray();

            response =
                "Handler whitelist:\n- " + (whitelist.Length > 0 ? string.Join("\n- ", whitelist) : "None") +
                "\n\nIgnored handlers:\n- " + (ignored.Length > 0 ? string.Join("\n- ", ignored) : "None") +
                "\n\nActive handlers:\n- " + (active.Length > 0 ? string.Join("\n- ", active) : "None") +
                "\n\nAvailable Exiled.API.Features classes:\n- " + string.Join("\n- ", classes);
            return true;
        }

        internal static bool ExecuteNetworkList(out string response)
        {
            var ignoredMethods = DebugManager.IgnoredNetworkMethods.OrderBy(x => x).ToArray();
            var activeMethods = DebugManager.KnownNetworkMethods.Where(x => !DebugManager.IgnoredNetworkMethods.Contains(x)).OrderBy(x => x).ToArray();
            var ignoredMessages = DebugManager.IgnoredNetworkMessages.OrderBy(x => x).ToArray();
            var activeMessages = DebugManager.KnownNetworkMessages.Where(x => !DebugManager.IgnoredNetworkMessages.Contains(x)).OrderBy(x => x).ToArray();

            response =
                "Ignored network methods:\n- " + (ignoredMethods.Length > 0 ? string.Join("\n- ", ignoredMethods) : "None") +
                "\n\nActive network methods:\n- " + (activeMethods.Length > 0 ? string.Join("\n- ", activeMethods) : "None") +
                "\n\nIgnored network messages:\n- " + (ignoredMessages.Length > 0 ? string.Join("\n- ", ignoredMessages) : "None") +
                "\n\nActive network messages:\n- " + (activeMessages.Length > 0 ? string.Join("\n- ", activeMessages) : "None");
            return true;
        }

        internal static bool ExecuteHandlerStart(ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            DebugManager.EnabledHandlerUsers.Add(player.UserId);
            HandlerLog.RegisterAllEvents();
            response = "Handler debug ON";
            return true;
        }

        internal static bool ExecuteHandlerStop(ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            DebugManager.EnabledHandlerUsers.Remove(player.UserId);
            response = "Handler debug OFF";
            return true;
        }

        internal static bool ExecuteNetworkStart(ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            DebugManager.EnabledNetworkUsers.Add(player.UserId);
            NetworkLog.RegisterAllEvents();
            response = "Network debug ON";
            return true;
        }

        internal static bool ExecuteNetworkStop(ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            DebugManager.EnabledNetworkUsers.Remove(player.UserId);
            response = "Network debug OFF";
            return true;
        }

        internal static bool ExecuteHandler(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = "Usage: GPDebugger handler <start/stop/list/ignore> [name]";
                return false;
            }

            string action = arguments.At(0);

            if (action == "start")
                return ExecuteHandlerStart(sender, out response);

            if (action == "stop")
                return ExecuteHandlerStop(sender, out response);

            if (action == "list")
                return ExecuteHandlerList(out response);

            if (action == "ignore")
            {
                if (arguments.Count < 3)
                {
                    response = "Usage: GPDebugger handler ignore <add/remove> <HandlerName>";
                    return false;
                }

                string ignoreAction = arguments.At(1);
                string handlerName = arguments.At(2);

                if (ignoreAction == "add")
                {
                    if (DebugManager.IgnoredHandlers.Add(handlerName))
                    {
                        response = $"Handler {handlerName} is now ignored.";
                        return true;
                    }

                    response = $"Handler {handlerName} is already ignored.";
                    return false;
                }

                if (ignoreAction == "remove")
                {
                    if (DebugManager.IgnoredHandlers.Remove(handlerName))
                    {
                        response = $"Handler {handlerName} removed from ignore list.";
                        return true;
                    }

                    response = $"Handler {handlerName} is not in the ignore list.";
                    return false;
                }

                response = "Invalid action. Use add/remove.";
                return false;
            }

            response = "Invalid action. Use start/stop/list/ignore.";
            return false;
        }

        internal static bool ExecuteNetwork(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = "Usage: GPDebugger network <start/stop/list/ignore>";
                return false;
            }

            string action = arguments.At(0);

            if (action == "start")
                return ExecuteNetworkStart(sender, out response);

            if (action == "stop")
                return ExecuteNetworkStop(sender, out response);

            if (action == "list")
                return ExecuteNetworkList(out response);

            if (action == "ignore")
            {
                if (arguments.Count < 3)
                {
                    response = "Usage: GPDebugger network ignore <add/remove> <Name>";
                    return false;
                }

                string ignoreAction = arguments.At(1);
                string name = arguments.At(2);
                bool knownMethod = DebugManager.KnownNetworkMethods.Contains(name);
                bool knownMessage = DebugManager.KnownNetworkMessages.Contains(name);

                if (ignoreAction == "add")
                {
                    if (!knownMethod && !knownMessage)
                    {
                        DebugManager.IgnoredNetworkMethods.Add(name);
                        DebugManager.IgnoredNetworkMessages.Add(name);
                    }
                    else
                    {
                        if (knownMethod) DebugManager.IgnoredNetworkMethods.Add(name);
                        if (knownMessage) DebugManager.IgnoredNetworkMessages.Add(name);
                    }

                    response = $"Network item {name} is now ignored.";
                    return true;
                }

                if (ignoreAction == "remove")
                {
                    var removedMethod = knownMethod ? DebugManager.IgnoredNetworkMethods.Remove(name) : DebugManager.IgnoredNetworkMethods.Remove(name);
                    var removedMessage = knownMessage ? DebugManager.IgnoredNetworkMessages.Remove(name) : DebugManager.IgnoredNetworkMessages.Remove(name);

                    if (removedMethod || removedMessage)
                    {
                        response = $"Network item {name} removed from ignore list.";
                        return true;
                    }

                    response = $"Network item {name} is not in the ignore list.";
                    return false;
                }

                response = "Invalid action. Use add/remove.";
                return false;
            }

            response = "Invalid action. Use start/stop/list/ignore.";
            return false;
        }

        internal static bool ExecuteIgnore(ArraySegment<string> arguments, out string response)
        {
            response = "Use gpdebug handler ignore or gpdebug network ignore.";
            return false;
        }

        internal static bool ExecutePrint(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);

            if (arguments.Count < 1)
            {
                response = "Usage: GPDebugger print <class/player/hit> [player] [component]\n- class: Print public static properties of an Exiled.API.Features class (e.g. Server, Map)\n- player: Print player properties (yourself or [player] if provided, optionally with [component])\n- hit: Print properties of the object you are looking at (Raycast), optionally with [component]";
                return false;
            }

            string targetTypeInfo = arguments.At(0).ToLower();

            if (targetTypeInfo == "hit")
            {
                var startPos = player.CameraTransform.position + player.CameraTransform.forward * 0.2f;
                if (UnityEngine.Physics.Raycast(startPos, player.CameraTransform.forward, out UnityEngine.RaycastHit hit, 100f))
                {
                    EnsureCacheInit();
                    var targetGo = hit.collider.gameObject;

                    if (arguments.Count >= 2)
                    {
                        string componentName = arguments.At(1);
                        var component = targetGo.GetComponent(componentName);
                        if (component != null)
                        {
                            response = PrintProperties(component.GetType(), component, $"--- {component.GetType().Name} Info ---");
                            player?.SendConsoleMessage(response, "white");
                            ServerConsole.AddLog(StripRichText(response));
                            return true;
                        }

                        response = $"Component '{componentName}' not found on {targetGo.name}.";
                        return false;
                    }

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
                            catch
                            {
                            }
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
                        sb.Append(PrintGameObjectComponents(targetGo));

                        response = sb.ToString();
                        player?.SendConsoleMessage(response, "white");
                        ServerConsole.AddLog(StripRichText(response));
                        return true;
                    }

                    response = PrintProperties(typeof(UnityEngine.GameObject), targetGo, $"--- GameObject Info: <color=#55aaff>{targetGo.name}</color> ---");
                    response += PrintGameObjectComponents(targetGo);
                    player?.SendConsoleMessage(response, "white");
                    ServerConsole.AddLog(StripRichText(response));
                    return true;
                }

                response = "You are not looking at anything.";
                return false;
            }

            if (targetTypeInfo == "player")
            {
                Player targetPlayer = player;
                string componentName = null;

                if (arguments.Count >= 2)
                {
                    string secondArg = arguments.At(1);
                    
                    if (arguments.Count >= 3)
                    {
                        targetPlayer = Player.Get(secondArg);
                        if (targetPlayer == null)
                        {
                            response = $"Player not found: {secondArg}";
                            return false;
                        }
                        componentName = arguments.At(2);
                    }
                    else
                    {
                        var testPlayer = Player.Get(secondArg);
                        if (testPlayer != null)
                        {
                            targetPlayer = testPlayer;
                        }
                        else
                        {
                            componentName = secondArg;
                        }
                    }
                }

                if (componentName != null && targetPlayer.GameObject != null)
                {
                    var component = targetPlayer.GameObject.GetComponent(componentName);
                    if (component != null)
                    {
                        response = PrintProperties(component.GetType(), component, $"--- {component.GetType().Name} Info ---");
                        player?.SendConsoleMessage(response, "white");
                        ServerConsole.AddLog(StripRichText(response));
                        return true;
                    }

                    response = $"Component '{componentName}' not found on player {targetPlayer.Nickname}.";
                    return false;
                }

                response = PrintProperties(typeof(Player), targetPlayer, $"--- Player Info: <color=#55aaff>{targetPlayer.Nickname}</color> ---");
                if (targetPlayer.GameObject != null)
                    response += PrintGameObjectComponents(targetPlayer.GameObject);
                player?.SendConsoleMessage(response, "white");
                ServerConsole.AddLog(StripRichText(response));
                return true;
            }

            var targetType = typeof(Server).Assembly.GetTypes()
                .FirstOrDefault(t => t.IsClass && t.Namespace == "Exiled.API.Features" && t.IsAbstract && t.IsSealed && t.Name.Equals(targetTypeInfo, StringComparison.OrdinalIgnoreCase));

            if (targetType != null)
            {
                response = PrintProperties(targetType, null, $"--- {targetType.Name} Info ---");
                player?.SendConsoleMessage(response, "white");
                ServerConsole.AddLog(StripRichText(response));
                return true;
            }

            response = $"Target '{targetTypeInfo}' not found in Exiled.API.Features or is not a public static class.";
            return false;
        }

        private static string PrintGameObjectComponents(UnityEngine.GameObject gameObject)
        {
            if (gameObject == null)
                return string.Empty;

            var components = gameObject.GetComponents<UnityEngine.Component>();
            if (components.Length == 0)
                return "\n<size=15>Components: <color=gray>None</color></size>";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("\n<size=15>Components:</size>");
            foreach (var component in components)
            {
                sb.AppendLine($"<size=15>- {component.GetType().Name}</size>");
            }
            return sb.ToString();
        }

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

        private static string StripRichText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return Regex.Replace(text, "<.*?>", string.Empty);
        }

        internal static string FormatValue(object val)
        {
            string valStr;
            if (val is bool b)
            {
                valStr = b ? "<color=green>True</color>" : "<color=red>False</color>";
            }
            else if (val is UnityEngine.Vector3 v3)
            {
                valStr = $"{v3} (new Vector3({v3.x.ToString("R", CultureInfo.InvariantCulture)}f, {v3.y.ToString("R", CultureInfo.InvariantCulture)}f, {v3.z.ToString("R", CultureInfo.InvariantCulture)}f);)";
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

        internal static string PrintProperties(Type type, object instance, string header)
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
                    bool isStatic = prop.GetMethod?.IsStatic ?? false;
                    string scopeLabel = isStatic
                        ? "<color=#4FC3F7>[Static]</color>"
                        : "<color=#81C784>[Instance]</color>";

                    object val = prop.GetValue(isStatic ? null : instance);
                    string valStr = FormatValue(val);
                    sb.AppendLine($"{scopeLabel} <b>{prop.Name}</b>: {valStr}");
                }
                catch (Exception ex)
                {
                    bool isStatic = prop.GetMethod?.IsStatic ?? false;
                    string scopeLabel = isStatic
                        ? "<color=#4FC3F7>[Static]</color>"
                        : "<color=#81C784>[Instance]</color>";

                    sb.AppendLine($"{scopeLabel} <b>{prop.Name}</b>: [Error] {ex.Message}");
                }
            }

            return "\n" + string.Join("\n", sb.ToString()
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => $"<size=15>{line}</size>"));
        }
    }
}
