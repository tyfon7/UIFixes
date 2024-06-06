using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using System.Reflection;

namespace UIFixes
{
    public class ContextMenuShortcutPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.Update));
        }

        [PatchPostfix]
        public static void Postfix(ItemUiContext __instance)
        {
            ItemContextAbstractClass itemContext = __instance.R().ItemContext;
            if (itemContext == null)
            {
                return;
            }

            if (Settings.InspectKeyBind.Value.IsDown())
            {
                __instance.GetItemContextInteractions(itemContext, null).ExecuteInteraction(EItemInfoButton.Inspect);
                return;
            }

            if (Settings.OpenKeyBind.Value.IsDown())
            {
                __instance.GetItemContextInteractions(itemContext, null).ExecuteInteraction(EItemInfoButton.Open);
                return;
            }

            if (Settings.TopUpKeyBind.Value.IsDown())
            {
                __instance.GetItemContextInteractions(itemContext, null).ExecuteInteraction(EItemInfoButton.TopUp);
                return;
            }

            if (Settings.FilterByKeyBind.Value.IsDown())
            {
                __instance.GetItemContextInteractions(itemContext, null).ExecuteInteraction(EItemInfoButton.FilterSearch);
                return;
            }

            if (Settings.LinkedSearchKeyBind.Value.IsDown())
            {
                __instance.GetItemContextInteractions(itemContext, null).ExecuteInteraction(EItemInfoButton.LinkedSearch);
                return;
            }
        }
    }
}
