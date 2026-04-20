using Exiled.API.Features;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GPDebugger.Features
{
    public static class NetworkLog
    {
        private static bool _isRegistered;

        public static void RegisterAllEvents()
        {
            if (_isRegistered)
                return;

            SeedKnownNetworkItems();

            var assembly = typeof(NetworkBehaviour).Assembly;
            var diagnosticsType = assembly.GetType("Mirror.NetworkDiagnostics");

            if (diagnosticsType != null)
            {
                var inMessageEvent = diagnosticsType.GetEvent("InMessageEvent", BindingFlags.Public | BindingFlags.Static);
                var outMessageEvent = diagnosticsType.GetEvent("OutMessageEvent", BindingFlags.Public | BindingFlags.Static);

                try
                {
                    if (inMessageEvent != null)
                        inMessageEvent.AddEventHandler(null, new Action<NetworkDiagnostics.MessageInfo>(OnInMessage));
                }
                catch { }

                try
                {
                    if (outMessageEvent != null)
                        outMessageEvent.AddEventHandler(null, new Action<NetworkDiagnostics.MessageInfo>(OnOutMessage));
                }
                catch { }
            }

            NetworkPatcher.EnsurePatched();
            _isRegistered = true;
        }

        private static void SeedKnownNetworkItems()
        {
            foreach (var type in GetLoadableTypes(typeof(NetworkBehaviour).Assembly))
            {
                if (type == null || !type.IsClass || type.IsAbstract)
                    continue;

                if (typeof(NetworkMessage).IsAssignableFrom(type))
                    DebugManager.KnownNetworkMessages.Add(type.Name);
            }

            foreach (var assembly in GetCandidateGameAssemblies())
            {
                foreach (var type in GetLoadableTypes(assembly))
                {
                    if (type == null || !type.IsClass)
                        continue;

                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    {
                        if (HasNetworkAttribute(method))
                            DebugManager.KnownNetworkMethods.Add(method.Name);
                    }
                }
            }
        }

        private static IEnumerable<Assembly> GetCandidateGameAssemblies()
        {
            var names = new[] { "Assembly-CSharp-Publicized", "Assembly-CSharp", "Assembly-CSharp-firstpass" };

            foreach (var name in names)
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase));

                if (assembly != null)
                    yield return assembly;
            }
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            if (assembly == null)
                yield break;

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            foreach (var type in types)
            {
                if (type != null)
                    yield return type;
            }
        }

        private static bool HasNetworkAttribute(MethodInfo method)
        {
            if (method == null)
                return false;

            foreach (var attr in method.GetCustomAttributesData())
            {
                var name = attr.AttributeType.Name;
                if (string.Equals(name, "CommandAttribute", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "ClientRpcAttribute", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "TargetRpcAttribute", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void OnInMessage(NetworkDiagnostics.MessageInfo info)
        {
            if (!ShouldLog())
                return;

            var messageName = info.message?.GetType().Name ?? "Unknown";
            DebugManager.KnownNetworkMessages.Add(messageName);

            if (IsIgnoredNetworkMessage(messageName))
                return;

            LogToViewers($"[NETWORK][IN] <color=#81C784>{messageName}</color> bytes={info.bytes} count={info.count} channel={info.channel}");
        }

        private static void OnOutMessage(NetworkDiagnostics.MessageInfo info)
        {
            if (!ShouldLog())
                return;

            var messageName = info.message?.GetType().Name ?? "Unknown";
            DebugManager.KnownNetworkMessages.Add(messageName);

            if (IsIgnoredNetworkMessage(messageName))
                return;

            LogToViewers($"[NETWORK][OUT] <color=#4FC3F7>{messageName}</color> bytes={info.bytes} count={info.count} channel={info.channel}");
        }

        private static void PostfixSendCommandInternal(NetworkBehaviour __instance, object[] __args)
        {
            var functionFullName = GetStringArg(__args, 0);
            var functionHashCode = GetIntArg(__args, 1);
            var channelId = GetIntArg(__args, 3);
            var requiresAuthority = GetBoolArg(__args, 4);
            LogNetworkMethod("CMD", __instance, functionFullName, functionHashCode, channelId, requiresAuthority);
        }

        private static void PostfixSendRPCInternal(NetworkBehaviour __instance, object[] __args)
        {
            var functionFullName = GetStringArg(__args, 0);
            var functionHashCode = GetIntArg(__args, 1);
            var channelId = GetIntArg(__args, 3);
            var includeOwner = GetBoolArg(__args, 4);
            LogNetworkMethod("RPC", __instance, functionFullName, functionHashCode, channelId, includeOwner);
        }

        private static void PostfixSendTargetRPCInternal(NetworkBehaviour __instance, object[] __args)
        {
            var target = GetArg<NetworkConnection>(__args, 0);
            var functionFullName = GetStringArg(__args, 1);
            var functionHashCode = GetIntArg(__args, 2);
            var channelId = GetIntArg(__args, 4);
            LogNetworkMethod("TARGET", __instance, functionFullName, functionHashCode, channelId, false, target);
        }

        private static void LogNetworkMethod(string kind, NetworkBehaviour instance, string functionFullName, int functionHash, int channelId, bool requiresAuthority, NetworkConnection target = null)
        {
            if (!ShouldLog() || instance == null)
                return;

            if (!string.IsNullOrWhiteSpace(functionFullName))
                DebugManager.KnownNetworkMethods.Add(functionFullName);

            if (IsIgnoredNetworkMethod(functionFullName, kind))
                return;

            string targetInfo = target == null ? string.Empty : $", target={target.connectionId}";
            LogToViewers($"[NETWORK][{kind}] <color=#55aaff>{instance.GetType().Name}</color>.{functionFullName} hash={functionHash} channel={channelId} auth={requiresAuthority}{targetInfo}");
        }

        private static string GetStringArg(object[] args, int index)
        {
            if (args == null || index < 0 || index >= args.Length)
                return string.Empty;

            return args[index]?.ToString() ?? string.Empty;
        }

        private static int GetIntArg(object[] args, int index)
        {
            if (args == null || index < 0 || index >= args.Length)
                return 0;

            if (args[index] == null)
                return 0;

            try { return Convert.ToInt32(args[index]); }
            catch { return 0; }
        }

        private static bool GetBoolArg(object[] args, int index)
        {
            if (args == null || index < 0 || index >= args.Length)
                return false;

            if (args[index] is bool b)
                return b;

            return false;
        }

        private static T GetArg<T>(object[] args, int index) where T : class
        {
            if (args == null || index < 0 || index >= args.Length)
                return null;

            return args[index] as T;
        }

        private static bool ShouldLog() => DebugManager.EnabledNetworkUsers.Count > 0;

        private static bool IsIgnoredNetworkMethod(string functionFullName, string kind)
        {
            if (string.IsNullOrWhiteSpace(functionFullName))
                return false;

            return DebugManager.IgnoredNetworkMethods.Contains(functionFullName) || DebugManager.IgnoredNetworkMethods.Contains(kind);
        }

        private static bool IsIgnoredNetworkMessage(string messageName)
        {
            if (string.IsNullOrWhiteSpace(messageName))
                return false;

            return DebugManager.IgnoredNetworkMessages.Contains(messageName);
        }

        private static void LogToViewers(string message)
        {
            foreach (var playerUserId in DebugManager.EnabledNetworkUsers.ToList())
            {
                var player = Player.Get(playerUserId);
                if (player == null)
                    continue;

                player.SendConsoleMessage(message, Main.Instance?.Config.ConsoleMessageColor ?? "white");
            }
        }
    }
}
