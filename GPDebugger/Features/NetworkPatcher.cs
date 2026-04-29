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

            MethodBase sendCommandInternal = typeof(NetworkBehaviour).GetMethod("SendCommandInternal", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodBase sendRpcInternal = typeof(NetworkBehaviour).GetMethod("SendRPCInternal", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodBase sendTargetRpcInternal = typeof(NetworkBehaviour).GetMethod("SendTargetRPCInternal", BindingFlags.NonPublic | BindingFlags.Instance);

            MethodInfo postfixCommand = typeof(NetworkLog).GetMethod("PostfixSendCommandInternal", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo postfixRpc = typeof(NetworkLog).GetMethod("PostfixSendRPCInternal", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo postfixTargetRpc = typeof(NetworkLog).GetMethod("PostfixSendTargetRPCInternal", BindingFlags.NonPublic | BindingFlags.Static);

            PatchMethod(sendCommandInternal, postfixCommand);
            PatchMethod(sendRpcInternal, postfixRpc);
            PatchMethod(sendTargetRpcInternal, postfixTargetRpc);

            _isPatched = true;
        }

        private static void PatchMethod(MethodBase original, MethodInfo postfix)
        {
            if (original == null || postfix == null)
                return;

            _harmony.Patch(original, postfix: new HarmonyMethod(postfix));
        }
    }
}
