using System.Reflection;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class QuickMovePreviewPatches
{
    public static void Enable()
    {
        new InitPreviewPatch().Enable();
        new KillPreviewPatch().Enable();
    }

    public class InitPreviewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemView), nameof(ItemView.NewItemView));
        }

        [PatchPostfix]
        public static void Postfix(ItemView __instance, TraderControllerClass ___ItemController, ItemUiContext ___ItemUiContext)
        {
            var previewer = __instance.GetOrAddComponent<QuickMovePreview>();
            if (previewer != null)
            {
                previewer.Init(__instance.ItemContext, ___ItemController, ___ItemUiContext);
            }
        }
    }

    public class KillPreviewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemView), nameof(ItemView.Kill));
        }

        [PatchPostfix]
        public static void Postfix(ItemView __instance)
        {
            var previewer = __instance.GetComponent<QuickMovePreview>();
            if (previewer != null)
            {
                previewer.Kill();
            }
        }
    }
}