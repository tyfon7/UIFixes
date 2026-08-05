using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
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

        new AddTagNewItemPatch().Enable();
        new AddTagParsedItemPatch().Enable();
        new TagAdditionalTypePatch().Enable();
        new KeepTagsLocalPatch().Enable();
    }

    // Save the tag when enter is pressed
    public class OnEnterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(EditTagWindow), nameof(EditTagWindow.Show));
        }

        [PatchPostfix]
        public static void Postfix(EditTagWindow __instance)
        {
            __instance._tagInput.onSubmit.AddListener(value => __instance.Save());
            __instance._tagInput.ActivateInputField();
            __instance._tagInput.Select();
        }
    }

    // On narrow items, prioritize the tag over the caption
    public class TagsOverCaptionsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridItemView), nameof(GridItemView.ResizeTag));
        }

        [PatchPostfix]
        public static async void Postfix(GridItemView __instance, Task __result)
        {
            await __result;

            // Rerun logic with preferred priority. Running again rather than prefix overwrite because this also fixes the existing race condition
            __instance.TagName.gameObject.SetActive(false);
            __instance.Caption.gameObject.SetActive(true);
            await Task.Yield();
            RectTransform tagTransform = __instance._tagColor.rectTransform;
            float tagSpace = __instance.RectTransform.rect.width - __instance.Caption.renderedWidth - 2f;
            if (tagSpace < 40f)
            {
                tagTransform.sizeDelta = new Vector2(__instance.RectTransform.sizeDelta.x, tagTransform.sizeDelta.y);
                if (Settings.TagsOverCaptions.Value)
                {
                    __instance.TagName.gameObject.SetActive(true);
                    float tagSize = Mathf.Clamp(__instance.TagName.preferredWidth + 12f, 40f, __instance.RectTransform.sizeDelta.x - 2f);
                    tagTransform.sizeDelta = new Vector2(tagSize, __instance._tagColor.rectTransform.sizeDelta.y);

                    __instance.Caption.gameObject.SetActive(false);
                }
            }
            else
            {
                __instance.TagName.gameObject.SetActive(true);
                float tagSize = Mathf.Clamp(__instance.TagName.preferredWidth + 12f, 40f, tagSpace);
                tagTransform.sizeDelta = new Vector2(tagSize, __instance._tagColor.rectTransform.sizeDelta.y);
            }

            // Make sure it's on top of the image
            if (__instance._tagColor.transform.GetSiblingIndex() < __instance.MainImage.transform.GetSiblingIndex())
            {
                __instance._tagColor.transform.SetSiblingIndex(__instance.MainImage.transform.GetSiblingIndex() + 1);
            }
        }
    }

    public class TagAdditionalTypePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredProperty(typeof(Item), nameof(Item.ItemInteractionButtons)).GetMethod;
        }

        [PatchPostfix]
        public static IEnumerable<EItemInfoButton> Postfix(IEnumerable<EItemInfoButton> values, Item __instance)
        {
            foreach (var value in values)
            {
                yield return value;
            }

            if (!IsTaggingEnabled(__instance))
            {
                yield break;
            }

            // Ensure this item has a tag component
            TagComponent tag = __instance.GetItemComponent<TagComponent>();
            if (tag == null)
            {
                yield break;
            }

            yield return EItemInfoButton.Tag;

            if (!string.IsNullOrEmpty(tag.Name))
            {
                yield return EItemInfoButton.ResetTag;
            }

            yield break;
        }

    }

    // Adds a TagComponent to types when they are constructed completely new
    public class AddTagNewItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemFactory), nameof(ItemFactory.CreateItem));
        }

        [PatchPostfix]
        public static void Postfix(Item __result, object itemDiff)
        {
            // If itemDiff is null, there's no deserialization, so just create the component
            if (itemDiff == null && IsTaggingEnabled(__result))
            {
                __result.Components.Add(new TagComponent(__result));
            }
        }
    }

    // Adds a TagComponent to types when they are deserialized from json
    // Also populate it; BSG does some insane manual reflection here for deserialization and looks for the literal Tag property (which it won't find)
    public class AddTagParsedItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemDeserializer), nameof(ItemDeserializer.CreateItem));
        }

        [PatchPostfix]
        public static void Postfix(Item item, UnparsedData properties)
        {
            if (IsTaggingEnabled(item))
            {
                TagComponent tagComponent = new(item);
                item.Components.Add(tagComponent);

                var propDictionary = properties.JToken.ToObject<Dictionary<string, UnparsedData>>();
                if (propDictionary.TryGetValue("Tag", out UnparsedData tagProperty))
                {
                    tagProperty.ParseJsonTo(tagComponent.GetType(), tagComponent);
                }
            }
        }
    }

    // For Fika compat: When items are serialized and set to other clients, lots of (normally safe) assumptions are made, like what components items have
    // If the other client does not have UIFixes, it will puke trying to deserialize a tag component on an item type that doesn't normally have them
    // So don't serialize the tag. It's no big loss anyway.
    public class KeepTagsLocalPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemBinarySerializer), nameof(ItemBinarySerializer.SerializeComponent));
        }

        [PatchPrefix]
        public static bool Prefix(IItemComponent component, ref object __result)
        {
            if (component is TagComponent tagComponent)
            {
                if (tagComponent.Item is Backpack or Vest)
                {
                    __result = null;
                    return false;
                }
            }

            return true;
        }
    }

    private static bool IsTaggingEnabled<T>(T instance)
    {
        return instance switch
        {
            Backpack => Settings.TagBackpacks.Value,
            Vest => Settings.TagVests.Value,
            _ => false
        };
    }
}