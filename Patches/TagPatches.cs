using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes;

public static class TagPatches
{
    public static void Enable()
    {
        new OnEnterPatch().Enable();
        new TagsOverCaptionsPatch().Enable();
    }

    public class OnEnterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(EditTagWindow), nameof(EditTagWindow.Show));
        }

        [PatchPostfix]
        public static void Postfix(EditTagWindow __instance, ValidationInputField ____tagInput)
        {
            ____tagInput.onSubmit.AddListener(value => __instance.method_4());
        }
    }

    public class TagsOverCaptionsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridItemView), nameof(GridItemView.method_21));
        }

        [PatchPostfix]
        public static async void Postfix(GridItemView __instance, TextMeshProUGUI ___TagName, TextMeshProUGUI ___Caption, Image ____tagColor, Task __result)
        {
            await __result;

            // Rerun logic with preferred priority. Running again rather than prefix overwrite because this also fixes the existing race condition
            ___TagName.gameObject.SetActive(false);
            ___Caption.gameObject.SetActive(true);
            await Task.Yield();
            RectTransform tagTransform = ____tagColor.rectTransform;
            float tagSpace = __instance.RectTransform.sizeDelta.x - ___Caption.renderedWidth - 2f;
            if (tagSpace < 40f)
            {
                tagTransform.sizeDelta = new Vector2(__instance.RectTransform.sizeDelta.x, tagTransform.sizeDelta.y);
                if (Settings.TagsOverCaptions.Value)
                {
                    ___TagName.gameObject.SetActive(true);
                    float tagSize = Mathf.Clamp(___TagName.preferredWidth + 12f, 40f, __instance.RectTransform.sizeDelta.x - 2f);
                    tagTransform.sizeDelta = new Vector2(tagSize, ____tagColor.rectTransform.sizeDelta.y);

                    ___Caption.gameObject.SetActive(false);
                }
            }
            else
            {
                ___TagName.gameObject.SetActive(true);
                float tagSize = Mathf.Clamp(___TagName.preferredWidth + 12f, 40f, tagSpace);
                tagTransform.sizeDelta = new Vector2(tagSize, ____tagColor.rectTransform.sizeDelta.y);
            }
        }
    }
}