using System.Reflection;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UIFixes;

public class RotateKeybindPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(DraggedItemView), nameof(DraggedItemView.method_8));
    }

    // Rotate is hardcoded to R in this method, so just replace the whole thing
    [PatchPrefix]
    public static bool Prefix(DraggedItemView __instance, IContainer ___iContainer, ItemContextAbstractClass ___itemContextAbstractClass)
    {
        if (Settings.RotateKeyBind.Value.IsDown())
        {
            __instance.method_2((__instance.ItemContext.ItemRotation == ItemRotation.Horizontal) ? ItemRotation.Vertical : ItemRotation.Horizontal);

            // BSG does this on mouse move, but forgot to do it on rotate, so the highlighted position is way off. Now better!
            var rectTransform = __instance.transform.RectTransform();
            Vector2 position = __instance.transform.position;
            Vector2 offset = rectTransform.rect.size * rectTransform.pivot * rectTransform.lossyScale;
            __instance.ItemContext.SetPosition(position, position - offset);

            if (___iContainer != null)
            {
                ___iContainer.HighlightItemViewPosition(__instance.ItemContext, ___itemContextAbstractClass, false);
            }
        }

        return false;
    }
}