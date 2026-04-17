using Exiled.API.Features;
using GPDebugger.Core.Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GPDebugger.Core.Feature
{
    public static class HandlerLog
    {
        public static bool IsRegistered = false;
        public static Dictionary<Type, string> HandlersMap = new();

        public static void RegisterAllEvents()
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

                        var method = typeof(HandlerLog)
                            .GetMethod(nameof(GenericHandler))
                            .MakeGenericMethod(paramType);

                        var del = Delegate.CreateDelegate(handlerType, method);

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

                        var method = typeof(HandlerLog)
                            .GetMethod(nameof(GenericHandler))
                            .MakeGenericMethod(paramType);

                        var del = Delegate.CreateDelegate(handlerType, method);

                        subscribeMethod.Invoke(eventObj, new object[] { del });

                        HandlersMap[paramType] = handlerName;
                    }
                    catch { }
                }
            }

            IsRegistered = true;
        }

        public static void GenericHandler<T>(T ev)
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

            string evString = ev?.ToString() ?? "null";
            int limit = Main.Instance.Config.ConsoleMessageLengthLimit;
            if (evString.Length > limit)
                evString = evString.Substring(0, limit) + "...";

            string header = $"[EVENT] <color=#55aaff>{handler}.{type.Name}</color>\n[ToString] {evString}";
            string message = Command.GPDebuggerCommand.PrintProperties(type, ev, header);

            foreach (var playerUserId in DebugManager.EnabledUsers.ToList())
            {
                Player player = Player.Get(playerUserId);
                player.SendConsoleMessage(message, Main.Instance.Config.ConsoleMessageColor);
            }
        }
    }
}
