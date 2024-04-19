using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI.WeaponModding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT.InputSystem;

namespace UIFixes
{
    public class WeaponBindingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type type = typeof(GClass960);
            return type.GetMethod("GetBoundItemNames", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void Postfix(GClass960 __instance, EBoundItem boundItem, ref string __result)
        {
            switch(boundItem)
            {
                case EBoundItem.Item1:
                    __result = __instance.GetKeyName(EGameKey.SecondaryWeapon);
                    break;
                case EBoundItem.Item2:
                    __result = __instance.GetKeyName(EGameKey.PrimaryWeaponFirst);
                    break;
                case EBoundItem.Item3:
                    __result = __instance.GetKeyName(EGameKey.PrimaryWeaponSecond);
                    break;
            }
        }
    }
}
