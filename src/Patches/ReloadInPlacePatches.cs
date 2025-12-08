using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UIFixes;

public static class ReloadInPlacePatches
{
    private static bool IsReloading = false;
    private static MagazineItemClass FoundMagazine = null;
    private static ItemAddress FoundAddress = null;

    public static void Enable()
    {
        // These patch ItemUiContext.ReloadWeapon, which is called from the context menu Reload
        new ReloadInPlacePatch().Enable();
        new ReloadInPlaceFindMagPatch().Enable();
        new ReloadInPlaceFindSpotPatch().Enable();
        new AlwaysSwapPatch().Enable();

        // This patches the firearmsController code when you hit R in raid with an external magazine class
        new SwapIfNoSpacePatch().Enable();
        //new InsertMagDebugPatch().Enable();
    }

    public class ReloadInPlacePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.ReloadWeapon));
        }

        [PatchPrefix]
        public static void Prefix()
        {
            IsReloading = Settings.SwapMags.Value;
        }

        [PatchPostfix]
        public static void Postfix()
        {
            IsReloading = false;
            FoundMagazine = null;
            FoundAddress = null;
        }
    }

    public class ReloadInPlaceFindMagPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.method_18));
        }

        [PatchPostfix]
        public static void Postfix(MagazineItemClass __result)
        {
            if (__result != null && IsReloading)
            {
                FoundMagazine = __result;
                FoundAddress = FoundMagazine.Parent;
            }
        }
    }

    public class ReloadInPlaceFindSpotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = typeof(ItemUiContext).GetNestedTypes().Single(t => t.GetField("currentMagazine") != null); // ItemUiContext.Class2775
            return AccessTools.Method(type, "method_0");
        }

        [PatchPrefix]
        public static void Prefix(StashGridClass grid, ref GStruct154<RemoveOperation> __state)
        {
            if (!Settings.SwapMags.Value)
            {
                return;
            }

            if (grid.Contains(FoundMagazine))
            {
                __state = InteractionsHandlerClass.Remove(FoundMagazine, grid.ParentItem.Owner as TraderControllerClass, false);
            }
        }

        [PatchPostfix]
        public static void Postfix(GStruct154<RemoveOperation> __state)
        {
            if (!Settings.SwapMags.Value || __state.Value == null)
            {
                return;
            }

            if (__state.Succeeded)
            {
                __state.Value.RollBack();
            }
        }
    }

    public class AlwaysSwapPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = typeof(ItemUiContext).GetNestedTypes().Single(t => t.GetField("func_3") != null); // ItemUiContext.Class2755
            return AccessTools.Method(type, "method_5");
        }

        [PatchPostfix]
        public static void Postfix(GridItemAddress g, ref int __result)
        {
            if (!Settings.AlwaysSwapMags.Value)
            {
                return;
            }

            if (!g.Equals(FoundAddress))
            {
                // Addresses that aren't the found address get massive value increase so found address is sorted first
                __result += 1000;
            }
        }
    }

    public class SwapIfNoSpacePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            if (Plugin.FikaPresent())
            {
                Type type = Type.GetType("Fika.Core.Main.ClientClasses.HandsControllers.FikaClientFirearmController, Fika.Core");
                return AccessTools.Method(type, "ReloadMag");
            }

            return AccessTools.Method(typeof(Player.FirearmController), nameof(Player.FirearmController.ReloadMag));
        }

        // By default this method will do a series of removes and adds, but not swap, to reload
        // This tied to a different animation state machine sequence than Swap(), and is faster than Swap.
        // So only use Swap if *needed*, otherwise its penalizing all reload speeds
        [PatchPrefix]
        public static bool Prefix(Player.FirearmController __instance, MagazineItemClass magazine, ItemAddress itemAddress, Callback callback, Player ____player)
        {
            // If itemAddress isn't null, it already found a place for the current mag, so let it run (unless always swap is enabled)
            if (!Settings.SwapMags.Value || (itemAddress != null && !Settings.AlwaysSwapMags.Value))
            {
                return true;
            }

            if (__instance.Blindfire)
            {
                return false;
            }

            ____player.RemoveLeftHandItem(3f);
            ____player.MovementContext.PlayerAnimator.AnimatedInteractions.ForceStopInteractions();
            if (____player.MovementContext.PlayerAnimator.AnimatedInteractions.IsInteractionPlaying)
            {
                return false;
            }

            if (!__instance.CanStartReload())
            {
                callback?.Fail("Cant StartReload");
                return false;
            }

            // Weapon doesn't currently have a magazine, let the default run (will load one)
            MagazineItemClass currentMagazine = __instance.Weapon.GetCurrentMagazine();
            if (currentMagazine == null)
            {
                return true;
            }

            InventoryController controller = __instance.Weapon.Owner as InventoryController;
            ItemAddress magAddress = magazine.Parent;

            // Null address means it couldn't find a spot. Try to remove magazine (temporarily) and try again
            var operation = InteractionsHandlerClass.Remove(magazine, controller, false);
            if (operation.Failed)
            {
                return true;
            }

            itemAddress = controller.Inventory.Equipment.GetPrioritizedGridsForUnloadedObject(false)
                .Select(grid => grid.FindLocationForItem(currentMagazine))
                .Where(address => address != null)
                .OrderByDescending(address => Settings.AlwaysSwapMags.Value && address.Equals(magAddress)) // Prioritize swapping if desired
                .OrderBy(address => address.Grid.GridWidth * address.Grid.GridHeight)
                .FirstOrDefault(); // BSG's version checks null again, but there's no nulls already. If there's no matches, the enumerable is empty

            // Put the magazine back
            operation.Value.RollBack();

            if (itemAddress == null)
            {
                // Didn't work, nowhere to put magazine. Let it run (will drop mag on ground)
                return true;
            }

            controller.TryRunNetworkTransaction(
                InteractionsHandlerClass.Swap(currentMagazine, itemAddress, magazine, __instance.Weapon.GetMagazineSlot().CreateItemAddress(), controller, true),
                callback);
            return false;
        }
    }

    // Dumps the animator parameters, lol. Desparate times and all that
    public class InsertMagDebugPatch : ModulePatch
    {
        private static FieldInfo ParameterListField;

        protected override MethodBase GetTargetMethod()
        {
            ParameterListField = AccessTools.Field(typeof(AnimatorWrapper), "animatorControllerParameter_0");
            return AccessTools.DeclaredMethod(typeof(FirearmInsertedMagState), nameof(FirearmInsertedMagState.Start));
        }

        [PatchPrefix]
        public static void Prefix(FirearmsAnimator ___firearmsAnimator_0)
        {
            StringBuilder sb = new();
            if (___firearmsAnimator_0.Animator is AnimatorWrapper animator)
            {
                for (int i = 0; i < animator.parameterCount; i++)
                {
                    AnimatorParameterInfo paramInfo = animator.GetParameter(i);
                    string name = animator.GetParameterName(paramInfo.nameHash);
                    string value = paramInfo.type switch
                    {
                        AnimatorControllerParameterType.Bool => animator.GetBool(paramInfo.nameHash).ToString(),
                        AnimatorControllerParameterType.Float => animator.GetFloat(paramInfo.nameHash).ToString(),
                        AnimatorControllerParameterType.Int => animator.GetInteger(paramInfo.nameHash).ToString(),
                        _ => "Unknown",
                    };

                    sb.AppendLine($"{name} ({paramInfo.type}) = {value}");
                }

                Plugin.Instance.Logger.LogInfo(sb.ToString());
            }
        }
    }
}