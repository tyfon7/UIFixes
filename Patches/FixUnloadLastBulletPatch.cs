using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;

namespace UIFixes
{
    public class FixUnloadLastBulletPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderControllerClass), nameof(TraderControllerClass.HasForeignEvents));
        }

        // Reimplement because theirs is wrong
        [PatchPrefix]
        public static bool Prefix(TraderControllerClass __instance, Item item, TraderControllerClass anotherOwner, ref bool __result, List<Item> ___list_2)
        {
            if (__instance == anotherOwner)
            {
                __result = false;
                return false;
            }

            ___list_2.Clear();
            item.GetAllItemsNonAlloc(___list_2, false, true);
            foreach (Item item2 in ___list_2)
            {
                foreach (var eventArgs in __instance.List_0)
                {
                    ItemAddress location = eventArgs.GetLocation();
                    if (!eventArgs.OwnerId.Equals(anotherOwner.ID) && !eventArgs.OwnerId.Equals(__instance.ID)) // checking against this is what I changed
                    {
                        if (item2 == eventArgs.Item)
                        {
                            __result = true;
                            return false;
                        }
                        if (location != null && location.Container.ParentItem == item2)
                        {
                            __result = true;
                            return false;
                        }
                    }
                }
            }
            __result = false;
            return false;
        }
    }
}
