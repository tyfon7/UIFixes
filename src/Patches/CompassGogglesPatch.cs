using System.Reflection;
using AnimationEventSystem;
using EFT;
using EFT.ItemInHandSubsystem;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public class CompassGogglesPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player), nameof(Player.ToggleGoggles));
    }

    [PatchPrefix]
    public static bool Prefix(Player __instance, ILeftHandController ____leftHandController)
    {
        if (____leftHandController.IsUsing)
        {
            __instance.RemoveLeftHandItem();

            Player.ItemHandsController itemHandsController = __instance.HandsController as Player.ItemHandsController;
            if (itemHandsController != null)
            {
                itemHandsController.SetCompassState(false);
            }

            if (____leftHandController is LeftHandController leftHandController)
            {
                Continuation continuation = new(__instance, leftHandController);
                leftHandController._events.OnActionEndedEvent += continuation.Continue;
            }

            return false;
        }

        return true;
    }

    private class Continuation(Player player, LeftHandController leftHandController)
    {
        public void Continue(IAnimatorEventParameter param)
        {
            leftHandController._events.OnActionEndedEvent -= this.Continue;

            player.ToggleGoggles();
        }
    }
}