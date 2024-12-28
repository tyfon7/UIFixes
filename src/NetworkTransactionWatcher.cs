using Comfort.Common;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace UIFixes;

public static class NetworkTransactionWatcher
{
    private static WatchedCallback Watcher;

    public static void Enable()
    {
        new NetworkTransactionPatch().Enable();
    }

    public static Task WatchNextTransaction()
    {
        if (Watcher != null)
        {
            throw new InvalidOperationException("UIFixes NetworkTransactionWatcher: Next transaction is already being watched!");
        }

        Watcher = new();
        return Watcher.Task;
    }

    public class NetworkTransactionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderControllerClass), nameof(TraderControllerClass.RunNetworkTransaction));
        }

        [PatchPrefix]
        [HarmonyPriority(Priority.First)]
        public static void Prefix(ref Callback callback)
        {
            Watcher?.Init(ref callback);
        }
    }

    private class WatchedCallback
    {
        private Callback innerCallback;
        private readonly TaskCompletionSource source = new();

        public Task Task => source.Task;

        public void Init(ref Callback callback)
        {
            innerCallback = callback;
            callback = Callback;
        }

        private void Callback(IResult result)
        {
            innerCallback?.Invoke(result);
            source.Complete();
            Watcher = null;
        }
    }
}
