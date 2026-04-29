using Exiled.API.Features;
using GPDebugger.Commands.GPDebugger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GPDebugger.Features
{
    public static class HandlerLog
    {
        public static bool IsRegistered = false;
        public static Dictionary<Type, string> HandlersMap = new();

        public static void RegisterAllEvents()
        {
            if (IsRegistered)
                return;

            System.Reflection.Assembly assembly = typeof(Exiled.Events.Handlers.Player).Assembly;

            Type[] types = assembly.GetTypes();
            foreach (Type type in types)
            {
                if (!type.IsClass)
                    continue;

                if (string.IsNullOrEmpty(type.Namespace) || !type.Namespace.StartsWith("Exiled.Events.Handlers"))
                    continue;

                string handlerName = type.Name;
                DebugManager.KnownHandlers.Add(handlerName);

                System.Reflection.EventInfo[] events = type.GetEvents(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                foreach (System.Reflection.EventInfo eventInfo in events)
                {
                    try
                    {
                        Type handlerType = eventInfo.EventHandlerType;
                        System.Reflection.MethodInfo invoke = handlerType.GetMethod("Invoke");
                        System.Reflection.ParameterInfo[] parameters = invoke.GetParameters();

                        if (parameters.Length != 1)
                            continue;

                        Type paramType = parameters[0].ParameterType;

                        System.Reflection.MethodInfo method = typeof(HandlerLog)
                            .GetMethod(nameof(GenericHandler))
                            .MakeGenericMethod(paramType);

                        Delegate del = Delegate.CreateDelegate(handlerType, method);

                        eventInfo.AddEventHandler(null, del);

                        HandlersMap[paramType] = handlerName;
                    }
                    catch { }
                }

                System.Reflection.PropertyInfo[] properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                foreach (System.Reflection.PropertyInfo propInfo in properties)
                {
                    try
                    {
                        Type propType = propInfo.PropertyType;
                        if (!propType.Name.StartsWith("Event"))
                            continue;

                        object eventObj = propInfo.GetValue(null);
                        if (eventObj == null)
                            continue;

                        System.Reflection.MethodInfo subscribeMethod = propType.GetMethods().FirstOrDefault(m =>
                            m.Name == "Subscribe" &&
                            m.GetParameters().Length == 1 &&
                            m.GetParameters()[0].ParameterType.Name.StartsWith("CustomEventHandler"));

                        if (subscribeMethod == null)
                            continue;

                        Type handlerType = subscribeMethod.GetParameters()[0].ParameterType;
                        System.Reflection.MethodInfo invoke = handlerType.GetMethod("Invoke");
                        System.Reflection.ParameterInfo[] parameters = invoke.GetParameters();

                        if (parameters.Length != 1)
                            continue;

                        Type paramType = parameters[0].ParameterType;

                        System.Reflection.MethodInfo method = typeof(HandlerLog)
                            .GetMethod(nameof(GenericHandler))
                            .MakeGenericMethod(paramType);

                        Delegate del = Delegate.CreateDelegate(handlerType, method);

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
            if (DebugManager.EnabledHandlerUsers.Count == 0)
                return;

            Type type = typeof(T);

            if (!HandlersMap.TryGetValue(type, out string handler))
                handler = "Unknown";

            if (!DebugManager.IsHandlerEnabled(handler))
                return;

            string eventName = $"{handler}.{type.Name}";
            if (DebugManager.IgnoredEvents.Contains(eventName))
                return;

            string evString = ev?.ToString() ?? "null";
            int limit = Main.Instance.Config.ConsoleMessageLengthLimit;
            if (evString.Length > limit)
                evString = evString.Substring(0, limit) + "...";
            string header = $"[EVENT] <color=#55aaff>{eventName}</color>\n[ToString] {evString}";
            string message = SubCommandHelper.PrintProperties(type, ev, header);

            List<string> enabledUsers = DebugManager.EnabledHandlerUsers.ToList();
            foreach (string playerUserId in enabledUsers)
            {
                Player player = Player.Get(playerUserId);
                if (player == null)
                    continue;

                player.SendConsoleMessage(message, Main.Instance.Config.ConsoleMessageColor);
            }
        }
    }
}
