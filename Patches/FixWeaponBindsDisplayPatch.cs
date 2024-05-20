using Aki.Reflection.Patching;
using EFT.InputSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Reflection;

namespace UIFixes
{
    public class FixWeaponBindsDisplayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(R.ControlSettings.Type, "GetBoundItemNames");
        }

        [PatchPostfix]
        public static void Postfix(object __instance, EBoundItem boundItem, ref string __result)
        {
            var instance = new R.ControlSettings(__instance);
            switch(boundItem)
            {
                case EBoundItem.Item1:
                    __result = instance.GetKeyName(EGameKey.SecondaryWeapon);
                    break;
                case EBoundItem.Item2:
                    __result = instance.GetKeyName(EGameKey.PrimaryWeaponFirst);
                    break;
                case EBoundItem.Item3:
                    __result = instance.GetKeyName(EGameKey.PrimaryWeaponSecond);
                    break;
            }
        }
    }
}
