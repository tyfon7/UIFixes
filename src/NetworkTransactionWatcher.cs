using Comfort.Common;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace UIFixes;

public class NetworkTransactionWatcher : IDisposable
{
    private static readonly Stack<NetworkTransactionWatcher> Watchers = [];

    public static void Enable()
    {
        new NetworkTransactionPatch().Enable();
    }

    public static NetworkTransactionWatcher WatchNext()
    {
        var watcher = new NetworkTransactionWatcher();
        Watchers.Push(watcher);
        return watcher;
    }

    private Callback innerCallback;
    private readonly TaskCompletionSource source = new();

    public Task Task => source.Task;

    protected void Watch(ref Callback callback)
    {
        innerCallback = callback;
        callback = Callback;
    }

    public void Dispose()
    {
        if (Watchers.Count > 0 && Watchers.Peek() == this)
        {
            Watchers.Pop();
            source.TrySetCanceled();
        }
    }

    private void Callback(IResult result)
    {
        innerCallback?.Invoke(result);
        source.TryComplete();
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
            if (Watchers.Count > 0)
            {
                Watchers.Pop().Watch(ref callback);
            }
        }
    }
}
