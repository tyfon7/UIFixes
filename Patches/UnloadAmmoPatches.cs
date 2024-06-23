using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UIFixes
{
    public static class UnloadAmmoPatches
    {
        public static void Enable()
        {
            new TradingPlayerPatch().Enable();
            new TransferPlayerPatch().Enable();
        }

        public class TradingPlayerPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.DeclaredProperty(typeof(GClass3032), nameof(GClass3032.AvailableInteractions)).GetMethod;
            }

            [PatchPostfix]
            public static void Postfix(ref IEnumerable<EItemInfoButton> __result)
            {
                var list = __result.ToList();
                list.Insert(list.IndexOf(EItemInfoButton.Repair), EItemInfoButton.UnloadAmmo);
                __result = list;
            }
        }

        public class TransferPlayerPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.DeclaredProperty(typeof(GClass3035), nameof(GClass3035.AvailableInteractions)).GetMethod;
            }

            [PatchPostfix]
            public static void Postfix(ref IEnumerable<EItemInfoButton> __result)
            {
                var list = __result.ToList();
                list.Insert(list.IndexOf(EItemInfoButton.Fold), EItemInfoButton.UnloadAmmo);
                __result = list;
            }
        }
    }
}
