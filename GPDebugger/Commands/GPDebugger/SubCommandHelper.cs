using CommandSystem;
using Exiled.API.Features;
using GPDebugger.Features;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace GPDebugger.Commands.GPDebugger
{
    internal static class SubCommandHelper
    {
        private static List<MethodInfo> _cachedGetMethods;

        #region Help

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
                "- <color=white>gpdebug pointer on/off</color>\n" +
                "  Shows live information about the Transform under your crosshair using HintServiceMeow.\n" +
                "- <color=white>gpdebug prefab spawn <PrefabType/enumIndex></color>\n" +
                "  Spawns a tracked network prefab at the point under your crosshair.\n" +
                "- <color=white>gpdebug prefab remove [ID/all]</color>\n" +
                "  Removes the tracked prefab under your crosshair, by ID, or all tracked prefabs.\n" +
                "- <color=white>gpdebug prefab list [filter]</color>\n" +
                "  Lists available PrefabType enum names.\n" +
                "- <color=white>gpdebug prefab lineup [spacing]</color>\n" +
                "  Spawns all prefab types in a straight showcase line with TextToy name labels.\n" +
                "- <color=white>gpdebug time pause/freeze/unfreeze/resume/status</color>\n" +
                "  Pauses global time, freezes the world except you, restores it, or displays status.\n" +
                "- <color=white>gpdebug time scale <0-10></color>\n" +
                "  Sets the server simulation time scale. A value of 0 pauses it.\n" +
                "- <color=white>gpdebug print <class/player/hit> [playerName] [componentName]</color>\n" +
                "  class: Prints public static properties of an Exiled.API.Features class (e.g. Server, Map).\n" +
                "  player: Prints player properties (self or target player). Optionally specify [componentName] to inspect a component.\n" +
                "  hit: Prints object info you are looking at. Optionally specify [componentName] to inspect a component.\n" +
                "  Examples: gpdebug print player, gpdebug print player 8 CharacterController, gpdebug print hit Rigidbody\n" +
                "- <color=white>gpdebug search <name></color>\n" +
                "  Searches scene Transform objects by name and lists position, scale, and bounds size.\n" +
                "- <color=white>gpdebug search <name> <number></color>\n" +
                "  Teleports you to the numbered search result.\n" +
                "  Aliases: gpdebug find <name>, gpdebug transform <name>, gpdebug tf <name>\n";
        }

        #endregion

        #region Handler

        internal static bool ExecuteHandlerStart(ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            DebugManager.EnabledHandlerUsers.Add(player.UserId);
            HandlerLog.RegisterAllEvents();
            response = "Handler debug ON";
            return true;
        }

        internal static bool ExecuteHandlerStop(ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            DebugManager.EnabledHandlerUsers.Remove(player.UserId);
            response = "Handler debug OFF";
            return true;
        }

        internal static bool ExecuteHandlerList(out string response)
        {
            string[] whitelist = DebugManager.HandlerWhitelist.OrderBy(x => x).ToArray();
            string[] ignored = DebugManager.IgnoredHandlers.OrderBy(x => x).ToArray();
            string[] active = DebugManager.KnownHandlers
                .Where(x => (DebugManager.HandlerWhitelist.Count == 0 || DebugManager.HandlerWhitelist.Contains(x)) && !DebugManager.IgnoredHandlers.Contains(x))
                .OrderBy(x => x)
                .ToArray();
            string[] classes = typeof(Server).Assembly.GetTypes()
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

        #endregion

        #region Network

        internal static bool ExecuteNetworkStart(ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            DebugManager.EnabledNetworkUsers.Add(player.UserId);
            NetworkLog.RegisterAllEvents();
            response = "Network debug ON";
            return true;
        }

        internal static bool ExecuteNetworkStop(ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            DebugManager.EnabledNetworkUsers.Remove(player.UserId);
            response = "Network debug OFF";
            return true;
        }

        internal static bool ExecuteNetworkList(out string response)
        {
            string[] ignoredMethods = DebugManager.IgnoredNetworkMethods.OrderBy(x => x).ToArray();
            string[] activeMethods = DebugManager.KnownNetworkMethods.Where(x => !DebugManager.IgnoredNetworkMethods.Contains(x)).OrderBy(x => x).ToArray();
            string[] ignoredMessages = DebugManager.IgnoredNetworkMessages.OrderBy(x => x).ToArray();
            string[] activeMessages = DebugManager.KnownNetworkMessages.Where(x => !DebugManager.IgnoredNetworkMessages.Contains(x)).OrderBy(x => x).ToArray();

            response =
                "Ignored network methods:\n- " + (ignoredMethods.Length > 0 ? string.Join("\n- ", ignoredMethods) : "None") +
                "\n\nActive network methods:\n- " + (activeMethods.Length > 0 ? string.Join("\n- ", activeMethods) : "None") +
                "\n\nIgnored network messages:\n- " + (ignoredMessages.Length > 0 ? string.Join("\n- ", ignoredMessages) : "None") +
                "\n\nActive network messages:\n- " + (activeMessages.Length > 0 ? string.Join("\n- ", activeMessages) : "None");
            return true;
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
                    bool removedMethod = DebugManager.IgnoredNetworkMethods.Remove(name);
                    bool removedMessage = DebugManager.IgnoredNetworkMessages.Remove(name);

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

        #endregion

        #region Ignore

        internal static bool ExecuteIgnore(ArraySegment<string> arguments, out string response)
        {
            response = "Use gpdebug handler ignore or gpdebug network ignore.";
            return false;
        }

        #endregion

        #region Pointer

        internal static bool ExecutePointer(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            if (player == null)
            {
                response = "Only an in-game player can use the pointer Transform inspector.";
                return false;
            }

            if (arguments.Count != 1)
            {
                response = "Usage: gpdebug pointer <on/off>";
                return false;
            }

            string action = arguments.At(0).ToLowerInvariant();
            if (action == "on")
            {
                bool newlyEnabled = TransformInspector.Start(player);
                response = newlyEnabled
                    ? "Pointer Transform inspector ON"
                    : "Pointer Transform inspector is already ON";
                return newlyEnabled;
            }

            if (action == "off")
            {
                bool wasEnabled = TransformInspector.Stop(player);
                response = wasEnabled
                    ? "Pointer Transform inspector OFF"
                    : "Pointer Transform inspector is already OFF";
                return wasEnabled;
            }

            response = "Usage: gpdebug pointer <on/off>";
            return false;
        }

        #endregion

        #region Prefab

        internal static bool ExecutePrefab(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = "Usage: gpdebug prefab <spawn/remove/list/lineup> [PrefabType/ID/all/filter/spacing]";
                return false;
            }

            string action = arguments.At(0).ToLowerInvariant();
            if (action == "list")
            {
                string filter = arguments.Count >= 2 ? arguments.At(1) : null;
                response = DebugPrefabManager.BuildPrefabList(filter);
                return true;
            }

            if (action == "spawn")
            {
                if (arguments.Count != 2)
                {
                    response = "Usage: gpdebug prefab spawn <PrefabType/enumIndex>";
                    return false;
                }

                Player player = Player.Get(sender);
                if (player == null)
                {
                    response = "Only an in-game player can choose a prefab spawn position.";
                    return false;
                }

                return DebugPrefabManager.Spawn(player, arguments.At(1), out response);
            }

            if (action == "lineup" || action == "showcase" || action == "gallery")
            {
                if (arguments.Count > 2)
                {
                    response = "Usage: gpdebug prefab lineup [spacing]";
                    return false;
                }

                float spacing = 5f;
                if (arguments.Count == 2 &&
                    !float.TryParse(arguments.At(1), NumberStyles.Float, CultureInfo.InvariantCulture, out spacing))
                {
                    response = "Spacing must be a number between 1 and 50 (example: gpdebug prefab lineup 6).";
                    return false;
                }

                Player player = Player.Get(sender);
                if (player == null)
                {
                    response = "Only an in-game player can choose the prefab lineup origin and direction.";
                    return false;
                }

                return DebugPrefabManager.StartLineup(player, spacing, out response);
            }

            if (action == "remove")
            {
                if (arguments.Count == 1)
                {
                    Player player = Player.Get(sender);
                    if (player == null)
                    {
                        response = "Only an in-game player can remove the prefab under the crosshair.";
                        return false;
                    }

                    return DebugPrefabManager.RemoveLookTarget(player, out response);
                }

                if (arguments.Count != 2)
                {
                    response = "Usage: gpdebug prefab remove [ID/all]";
                    return false;
                }

                string target = arguments.At(1);
                if (string.Equals(target, "all", StringComparison.OrdinalIgnoreCase))
                {
                    int removed = DebugPrefabManager.DestroyAll();
                    response = $"Removed {removed} GPDebugger prefab(s).";
                    return true;
                }

                if (!int.TryParse(target, NumberStyles.None, CultureInfo.InvariantCulture, out int id))
                {
                    response = "Prefab ID must be a positive number or 'all'.";
                    return false;
                }

                return DebugPrefabManager.Remove(id, out response);
            }

            response = "Usage: gpdebug prefab <spawn/remove/list/lineup> [PrefabType/ID/all/filter/spacing]";
            return false;
        }

        #endregion

        #region Time

        internal static bool ExecuteTime(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = "Usage: gpdebug time <pause/freeze/unfreeze/resume/scale/status> [0-10]";
                return false;
            }

            string action = arguments.At(0).ToLowerInvariant();
            if (action == "status")
            {
                string timeStatus = DebugTimeManager.IsPaused
                    ? "Global time: PAUSED (timeScale: 0)"
                    : $"Global timeScale: {DebugTimeManager.CurrentScale:0.###}";
                string freezeStatus = DebugWorldFreezeManager.IsActive
                    ? $"World freeze: ON (excluded: {DebugWorldFreezeManager.ExcludedNickname})"
                    : "World freeze: OFF";
                response = $"{timeStatus}\n{freezeStatus}";
                return true;
            }

            if (action == "freeze")
            {
                if (arguments.Count != 1)
                {
                    response = "Usage: gpdebug time freeze";
                    return false;
                }

                Player player = Player.Get(sender);
                return DebugWorldFreezeManager.Freeze(player, out response);
            }

            if (action == "unfreeze")
            {
                if (arguments.Count != 1)
                {
                    response = "Usage: gpdebug time unfreeze";
                    return false;
                }

                if (!DebugWorldFreezeManager.IsActive)
                {
                    response = "World freeze is not active.";
                    return false;
                }

                int restored = DebugWorldFreezeManager.Resume();
                response = $"World freeze disabled. Restored {restored} tracked state(s).";
                return true;
            }

            if (action == "pause")
            {
                if (arguments.Count != 1)
                {
                    response = "Usage: gpdebug time pause";
                    return false;
                }

                if (DebugTimeManager.IsPaused)
                {
                    response = "Server simulation time is already paused.";
                    return false;
                }

                DebugWorldFreezeManager.Resume();
                DebugTimeManager.TrySetScale(0f, out _);
                response = "Server simulation time PAUSED. Use 'gpdebug time resume' to restore it.";
                return true;
            }

            if (action == "resume")
            {
                if (arguments.Count != 1)
                {
                    response = "Usage: gpdebug time resume";
                    return false;
                }

                int restored = DebugWorldFreezeManager.Resume();
                DebugTimeManager.Restore();
                response = restored > 0
                    ? $"Server simulation time RESUMED (timeScale: 1). Restored {restored} world state(s)."
                    : "Server simulation time RESUMED (timeScale: 1).";
                return true;
            }

            if (action == "scale")
            {
                if (arguments.Count != 2 ||
                    !float.TryParse(arguments.At(1), NumberStyles.Float, CultureInfo.InvariantCulture, out float scale))
                {
                    response = "Usage: gpdebug time scale <0-10> (example: gpdebug time scale 0.25)";
                    return false;
                }

                if (!DebugTimeManager.TrySetScale(scale, out response))
                    return false;

                DebugWorldFreezeManager.Resume();

                response = scale == 0f
                    ? "Server simulation time PAUSED. Use 'gpdebug time resume' to restore it."
                    : $"Server simulation time scale set to {scale:0.###}.";
                return true;
            }

            response = "Usage: gpdebug time <pause/freeze/unfreeze/resume/scale/status> [0-10]";
            return false;
        }

        #endregion

        #region Search

        internal static bool ExecuteSearch(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);

            if (arguments.Count < 1)
            {
                response = "Usage: GPDebugger search <name>";
                return false;
            }

            bool shouldTeleport = TryParseSearchArguments(arguments, out string query, out int resultNumber);
            const int maxResults = 50;

            UnityEngine.Transform[] matches = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Transform>()
                .Where(transform => transform != null &&
                                    transform.gameObject != null &&
                                    transform.gameObject.scene.IsValid() &&
                                    transform.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(transform => GetTransformPath(transform))
                .ToArray();

            if (matches.Length == 0)
            {
                response = $"No Transform found with name containing '{query}'.";
                return false;
            }

            int listedCount = Math.Min(matches.Length, maxResults);
            if (shouldTeleport)
            {
                if (player == null)
                {
                    response = "Only an in-game player can teleport to a search result.";
                    return false;
                }

                if (resultNumber < 1 || resultNumber > listedCount)
                {
                    response = $"Search result number must be between 1 and {listedCount}.";
                    return false;
                }

                UnityEngine.Transform target = matches[resultNumber - 1];
                player.Position = target.position;
                response =
                    $"Teleported to search result #{resultNumber}: {target.name}\n" +
                    $"Path: {GetTransformPath(target)}\n" +
                    $"Position: {FormatVector3(target.position)}";
                player.SendConsoleMessage(response, "white");
                ServerConsole.AddLog(StripRichText(response));
                return true;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"--- Search: <color=#55aaff>{query}</color> ({matches.Length} Transform objects found) ---");
            if (matches.Length > maxResults)
                sb.AppendLine($"Showing first {maxResults} results.");

            for (int i = 0; i < listedCount; i++)
            {
                sb.AppendLine(FormatSearchResult(matches[i], i + 1));
            }

            response = sb.ToString();
            player?.SendConsoleMessage(response, "white");
            ServerConsole.AddLog(StripRichText(response));
            return true;
        }

        private static bool TryParseSearchArguments(ArraySegment<string> arguments, out string query, out int resultNumber)
        {
            resultNumber = 0;
            int queryArgumentCount = arguments.Count;

            if (arguments.Count >= 2 && int.TryParse(arguments.At(arguments.Count - 1), NumberStyles.None, CultureInfo.InvariantCulture, out int parsedNumber))
            {
                resultNumber = parsedNumber;
                queryArgumentCount--;
            }

            query = string.Join(" ", arguments.Array.Skip(arguments.Offset).Take(queryArgumentCount));
            return queryArgumentCount < arguments.Count;
        }

        private static string FormatSearchResult(UnityEngine.Transform transform, int number)
        {
            UnityEngine.GameObject gameObject = transform.gameObject;
            UnityEngine.Vector3? rendererSize = TryGetRendererBoundsSize(gameObject);
            UnityEngine.Vector3? colliderSize = TryGetColliderBoundsSize(gameObject);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<size=15>{number}. <b>{transform.name}</b> [{(gameObject.activeInHierarchy ? "Active" : "Inactive")}]</size>");
            sb.AppendLine($"<size=15>  Path: {GetTransformPath(transform)}</size>");
            sb.AppendLine($"<size=15>  Position: {FormatVector3(transform.position)} | Local: {FormatVector3(transform.localPosition)}</size>");
            sb.AppendLine($"<size=15>  Rotation: {FormatVector3(transform.eulerAngles)} | Local: {FormatVector3(transform.localEulerAngles)}</size>");
            sb.AppendLine($"<size=15>  Scale: {FormatVector3(transform.lossyScale)} | Local: {FormatVector3(transform.localScale)}</size>");
            sb.AppendLine($"<size=15>  Bounds Size: Renderer={FormatNullableVector3(rendererSize)}, Collider={FormatNullableVector3(colliderSize)}</size>");
            return sb.ToString();
        }

        private static string GetTransformPath(UnityEngine.Transform transform)
        {
            Stack<string> names = new Stack<string>();
            UnityEngine.Transform current = transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }

        private static UnityEngine.Vector3? TryGetRendererBoundsSize(UnityEngine.GameObject gameObject)
        {
            UnityEngine.Renderer[] renderers = gameObject.GetComponentsInChildren<UnityEngine.Renderer>(true);
            if (renderers.Length == 0)
                return null;

            UnityEngine.Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds.size;
        }

        private static UnityEngine.Vector3? TryGetColliderBoundsSize(UnityEngine.GameObject gameObject)
        {
            UnityEngine.Collider[] colliders = gameObject.GetComponentsInChildren<UnityEngine.Collider>(true);
            if (colliders.Length == 0)
                return null;

            UnityEngine.Bounds bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }

            return bounds.size;
        }

        private static string FormatNullableVector3(UnityEngine.Vector3? value)
        {
            return value.HasValue ? FormatVector3(value.Value) : "None";
        }

        private static string FormatVector3(UnityEngine.Vector3 value)
        {
            return $"({value.x.ToString("0.###", CultureInfo.InvariantCulture)}, {value.y.ToString("0.###", CultureInfo.InvariantCulture)}, {value.z.ToString("0.###", CultureInfo.InvariantCulture)})";
        }

        #endregion

        #region Print

        internal static bool ExecutePrint(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);

            if (arguments.Count < 1)
            {
                response = "Usage: GPDebugger print <class/player/hit> [player] [component]\n- class: Print public static properties of an Exiled.API.Features class (e.g. Server, Map)\n- player: Print player properties (yourself or [player] if provided, optionally with [component])\n- hit: Print properties of the object you are looking at (Raycast), optionally with [component]";
                return false;
            }

            string targetTypeInfo = arguments.At(0).ToLower();

            if (targetTypeInfo == "hit")
            {
                UnityEngine.Vector3 startPos = player.CameraTransform.position + player.CameraTransform.forward * 0.2f;
                if (UnityEngine.Physics.Raycast(startPos, player.CameraTransform.forward, out UnityEngine.RaycastHit hit, 100f))
                {
                    UnityEngine.GameObject targetGo = hit.collider.gameObject;

                    if (arguments.Count >= 2)
                    {
                        string componentName = arguments.At(1);
                        UnityEngine.Component component = targetGo.GetComponent(componentName);
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

                    response = BuildHitInspection(hit);
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
                        Player testPlayer = Player.Get(secondArg);
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
                    UnityEngine.Component component = targetPlayer.GameObject.GetComponent(componentName);
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

            Type targetType = typeof(Server).Assembly.GetTypes()
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

        internal static string BuildHitInspection(UnityEngine.RaycastHit hit)
            => BuildObjectInspection(hit.collider.gameObject, hit.collider);

        internal static string BuildObjectInspection(UnityEngine.GameObject targetGo, UnityEngine.Collider hitCollider = null)
        {
            if (targetGo == null)
                return "Target GameObject is null.";

            EnsureCacheInit();
            HashSet<object> foundObjects = new HashSet<object>();
            UnityEngine.Transform currentTransform = targetGo.transform;

            while (currentTransform != null && foundObjects.Count == 0)
            {
                foreach (MethodInfo method in _cachedGetMethods)
                {
                    try
                    {
                        ParameterInfo[] methodParams = method.GetParameters();
                        Type paramType = methodParams[0].ParameterType;
                        object arg = null;

                        if (paramType == typeof(UnityEngine.GameObject)) arg = currentTransform.gameObject;
                        else if (paramType == typeof(UnityEngine.Transform)) arg = currentTransform;
                        else if (paramType == typeof(UnityEngine.Collider) &&
                                 hitCollider != null &&
                                 currentTransform == targetGo.transform) arg = hitCollider;

                        if (arg == null)
                            continue;

                        object result = method.Invoke(null, new[] { arg });
                        if (result != null)
                            foundObjects.Add(result);
                    }
                    catch
                    {
                    }
                }

                if (foundObjects.Count > 0)
                    break;

                currentTransform = currentTransform.parent;
            }

            if (foundObjects.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (object obj in foundObjects)
                    sb.AppendLine(PrintProperties(obj.GetType(), obj, $"--- {obj.GetType().Name} Info ---"));
                sb.Append(PrintGameObjectComponents(targetGo));
                return sb.ToString();
            }

            string response = PrintProperties(
                typeof(UnityEngine.GameObject),
                targetGo,
                $"--- GameObject Info: <color=#55aaff>{targetGo.name}</color> ---");
            return response + PrintGameObjectComponents(targetGo);
        }

        private static string PrintGameObjectComponents(UnityEngine.GameObject gameObject)
        {
            if (gameObject == null)
                return string.Empty;

            UnityEngine.Component[] components = gameObject.GetComponents<UnityEngine.Component>();
            if (components.Length == 0)
                return "\n<size=15>Components: <color=gray>None</color></size>";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n<size=15>Components:</size>");
            foreach (UnityEngine.Component component in components)
            {
                sb.AppendLine($"<size=15>- {component.GetType().Name}</size>");
            }
            return sb.ToString();
        }

        private static void EnsureCacheInit()
        {
            if (_cachedGetMethods != null) return;
            _cachedGetMethods = new List<MethodInfo>();

            Type[] types = typeof(Server).Assembly.GetTypes()
                .Where(t => t.IsClass && !string.IsNullOrEmpty(t.Namespace) && t.Namespace.StartsWith("Exiled.API.Features"))
                .ToArray();

            foreach (Type type in types)
            {
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (MethodInfo method in methods)
                {
                    if (method.Name == "Get" && method.GetParameters().Length == 1)
                    {
                        Type paramType = method.GetParameters()[0].ParameterType;
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
            else if (val is UnityEngine.Quaternion quaternion)
            {
                UnityEngine.Vector3 euler = quaternion.eulerAngles;
                valStr =
                    $"({FormatCompactFloat(quaternion.x)}, {FormatCompactFloat(quaternion.y)}, " +
                    $"{FormatCompactFloat(quaternion.z)}, {FormatCompactFloat(quaternion.w)}) " +
                    $"Euler={FormatCompactVector3(euler)}";
            }
            else if (val is UnityEngine.Matrix4x4 matrix)
            {
                valStr =
                    $"[{FormatCompactFloat(matrix.m00)}, {FormatCompactFloat(matrix.m01)}, {FormatCompactFloat(matrix.m02)}, {FormatCompactFloat(matrix.m03)}; " +
                    $"{FormatCompactFloat(matrix.m10)}, {FormatCompactFloat(matrix.m11)}, {FormatCompactFloat(matrix.m12)}, {FormatCompactFloat(matrix.m13)}; " +
                    $"{FormatCompactFloat(matrix.m20)}, {FormatCompactFloat(matrix.m21)}, {FormatCompactFloat(matrix.m22)}, {FormatCompactFloat(matrix.m23)}; " +
                    $"{FormatCompactFloat(matrix.m30)}, {FormatCompactFloat(matrix.m31)}, {FormatCompactFloat(matrix.m32)}, {FormatCompactFloat(matrix.m33)}]";
            }
            else if (val is System.Collections.IEnumerable enumerable && !(val is string))
            {
                List<string> items = new List<string>();
                foreach (object item in enumerable)
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
                    List<string> subItems = new List<string>();
                    PropertyInfo[] properties = val.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (PropertyInfo p in properties)
                    {
                        if (p.GetIndexParameters().Length > 0) continue;
                        try { subItems.Add($"{p.Name}: {p.GetValue(val)}"); } catch { }
                    }
                    FieldInfo[] fields = val.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                    foreach (FieldInfo f in fields)
                    {
                        try { subItems.Add($"{f.Name}: {f.GetValue(val)}"); } catch { }
                    }
                    if (subItems.Count > 0)
                        valStr = "{" + string.Join(", ", subItems) + "}";
                }
            }

            return valStr;
        }

        private static string FormatCompactFloat(float value)
            => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string FormatCompactVector3(UnityEngine.Vector3 value)
            => $"({FormatCompactFloat(value.x)}, {FormatCompactFloat(value.y)}, {FormatCompactFloat(value.z)})";

        internal static string PrintProperties(Type type, object instance, string header)
            => PrintPropertiesCore(type, instance, header);

        private static string PrintPropertiesCore(Type type, object instance, string header)
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(header))
                sb.AppendLine(header);

            BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            if (instance != null) flags |= BindingFlags.Instance;

            PropertyInfo[] properties = type.GetProperties(flags)
                .OrderBy(prop => string.Equals(prop.Name, "name", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ToArray();
            foreach (PropertyInfo prop in properties)
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

            string[] lines = sb.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string[] formatted = lines.Select(line => $"<size=15>{line}</size>").ToArray();
            return (string.IsNullOrWhiteSpace(header) ? string.Empty : "\n") + string.Join("\n", formatted);
        }

        #endregion
    }
}
