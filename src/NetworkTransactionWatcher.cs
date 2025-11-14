using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

// A watcher simply watches for the next TraderControllerClass.RunNetworkTransaction and resolves its task when 
// that method completes by calling its callback.
// This class also manages the watchers, ensuring they are called in reverse order (LIFO).
// To use simply call NetworkTransactionWatch.WatchNext()
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
        if (Watchers.Count > 0)
        {
            var watcher = Watchers.Pop();
            if (watcher != this)
            {
                throw new InvalidOperationException("NetworkTransactionWatcher disposed out of order");
            }

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
            while (Watchers.Count > 0)
            {
                Watchers.Pop().Watch(ref callback);
            }
        }
    }
}
