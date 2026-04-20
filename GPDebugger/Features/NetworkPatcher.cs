using HarmonyLib;
using Mirror;
using System.Reflection;

namespace GPDebugger.Features
{
    internal static class NetworkPatcher
    {
        private static bool _isPatched;
        private static Harmony _harmony;

        internal static void EnsurePatched()
        {
            if (_isPatched)
                return;

            _harmony = new Harmony("GPDebugger.NetworkLog");

            PatchMethod(typeof(NetworkBehaviour).GetMethod("SendCommandInternal", BindingFlags.NonPublic | BindingFlags.Instance), GetPostfix("PostfixSendCommandInternal"));
            PatchMethod(typeof(NetworkBehaviour).GetMethod("SendRPCInternal", BindingFlags.NonPublic | BindingFlags.Instance), GetPostfix("PostfixSendRPCInternal"));
            PatchMethod(typeof(NetworkBehaviour).GetMethod("SendTargetRPCInternal", BindingFlags.NonPublic | BindingFlags.Instance), GetPostfix("PostfixSendTargetRPCInternal"));

            _isPatched = true;
        }

        private static MethodInfo GetPostfix(string methodName)
        {
            return typeof(NetworkLog).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        }

        private static void PatchMethod(MethodBase original, MethodInfo postfix)
        {
            if (original == null || postfix == null)
                return;

            _harmony.Patch(original, postfix: new HarmonyMethod(postfix));
        }
    }
}
