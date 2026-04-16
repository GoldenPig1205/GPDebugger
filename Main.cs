using Exiled.API.Features;
using GPDebug.Core.Class;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPDebug
{
    public class Main : Plugin<Config>
    {
        public override string Name => "GPDebug";
        public override string Author => "GoldenPig1205";
        public override Version Version { get; } = new(1, 0, 0);
        public override Version RequiredExiledVersion { get; } = new(1, 2, 0, 5);

        public static Main Instance { get; set; }

        public bool IsRegistered = false;
        public Dictionary<Type, string> HandlersMap = new();

        public override void OnEnabled()
        {
            Instance = this;
            base.OnEnabled();

            DebugManager.IgnoredEvents.Clear();
            foreach (var ignored in Config.IgnoredEvents)
            {
                DebugManager.IgnoredEvents.Add(ignored);
            }

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
            DebugManager.EnabledUsers.Clear();
        }

        public void RegisterAllEvents()
        {
            if (IsRegistered)
                return;

            var assembly = typeof(Exiled.Events.Handlers.Player).Assembly;

            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsClass)
                    continue;

                if (string.IsNullOrEmpty(type.Namespace) || !type.Namespace.StartsWith("Exiled.Events.Handlers"))
                    continue;

                string handlerName = type.Name;

                foreach (var eventInfo in type.GetEvents(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                {
                    try
                    {
                        var handlerType = eventInfo.EventHandlerType;
                        var invoke = handlerType.GetMethod("Invoke");
                        var parameters = invoke.GetParameters();

                        if (parameters.Length != 1)
                            continue;

                        var paramType = parameters[0].ParameterType;

                        var method = GetType()
                            .GetMethod(nameof(GenericHandler))
                            .MakeGenericMethod(paramType);

                        var del = Delegate.CreateDelegate(handlerType, this, method);

                        eventInfo.AddEventHandler(null, del);

                        HandlersMap[paramType] = handlerName;
                    }
                    catch { }
                }

                foreach (var propInfo in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                {
                    try
                    {
                        var propType = propInfo.PropertyType;
                        if (!propType.Name.StartsWith("Event"))
                            continue;

                        var eventObj = propInfo.GetValue(null);
                        if (eventObj == null)
                            continue;

                        var subscribeMethod = propType.GetMethods().FirstOrDefault(m => 
                            m.Name == "Subscribe" && 
                            m.GetParameters().Length == 1 &&
                            m.GetParameters()[0].ParameterType.Name.StartsWith("CustomEventHandler"));

                        if (subscribeMethod == null)
                            continue;

                        var handlerType = subscribeMethod.GetParameters()[0].ParameterType;
                        var invoke = handlerType.GetMethod("Invoke");
                        var parameters = invoke.GetParameters();

                        if (parameters.Length != 1)
                            continue;

                        var paramType = parameters[0].ParameterType;

                        var method = GetType()
                            .GetMethod(nameof(GenericHandler))
                            .MakeGenericMethod(paramType);

                        var del = Delegate.CreateDelegate(handlerType, this, method);

                        subscribeMethod.Invoke(eventObj, new object[] { del });

                        HandlersMap[paramType] = handlerName;
                    }
                    catch { }
                }
            }

            IsRegistered = true;
        }

        public void GenericHandler<T>(T ev)
        {
            if (DebugManager.EnabledUsers.Count == 0)
                return;

            var type = typeof(T);

            if (!HandlersMap.TryGetValue(type, out var handler))
                handler = "Unknown";

            if (!DebugManager.IsHandlerEnabled(handler))
                return;

            string fullEventName = $"{handler}.{type.Name}";
            if (DebugManager.IgnoredEvents.Contains(fullEventName))
                return;

            foreach (var playerUserId in DebugManager.EnabledUsers.ToList())
            {
                Player player = Player.Get(playerUserId);

                var sb = new StringBuilder();
                int limit = Config.ConsoleMessageLengthLimit;

                sb.AppendLine($"[EVENT] <color=green>{handler}.{type.Name}</color>");

                string evString = ev?.ToString() ?? "null";
                if (evString.Length > limit)
                    evString = evString.Substring(0, limit) + "...";
                sb.AppendLine($"[ToString] {evString}");

                foreach (var prop in type.GetProperties())
                {
                    object value = null;

                    try
                    {
                        value = prop.GetValue(ev);
                    }
                    catch { }

                    string valueStr = value?.ToString() ?? "null";

                    if (valueStr.Length > limit)
                        valueStr = valueStr.Substring(0, limit) + "...";

                    sb.AppendLine($"- {prop.Name} ({prop.PropertyType.Name}): {valueStr}");
                }

                string finalMessage = "\n" + string.Join("\n", sb.ToString()
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => $"<size=15>{line}</size>"));

                player.SendConsoleMessage(finalMessage, Config.ConsoleMessageColor);
            }
        }
    }
}
