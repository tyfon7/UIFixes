using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace UIFixes
{
    internal class ItemPanelPatches
    {
        private static FieldInfo AttributeCompactPanelDictionaryField;
        private static FieldInfo AttributeCompactDropdownDictionaryField;

        private static FieldInfo ItemComponentItemField;

        public static void Enable()
        {
            AttributeCompactPanelDictionaryField = AccessTools.GetDeclaredFields(typeof(ItemSpecificationPanel)).First(f => typeof(IEnumerable<KeyValuePair<ItemAttributeClass, CompactCharacteristicPanel>>).IsAssignableFrom(f.FieldType));
            AttributeCompactDropdownDictionaryField = AccessTools.GetDeclaredFields(typeof(ItemSpecificationPanel)).First(f => typeof(IEnumerable<KeyValuePair<ItemAttributeClass, CompactCharacteristicDropdownPanel>>).IsAssignableFrom(f.FieldType));

            Type ItemComponentType = PatchConstants.EftTypes.First(t => typeof(IItemComponent).IsAssignableFrom(t) && AccessTools.Field(t, "Item") != null);
            ItemComponentItemField = AccessTools.Field(ItemComponentType, "Item");

            new InjectButtonPatch().Enable();
            new LoadModStatsPatch().Enable();
            new CompareModPatch().Enable();
            new FormatValuesPatch().Enable();
        }

        private class LoadModStatsPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemSpecificationPanel), "method_5");
            }

            [PatchPostfix]
            private static void Postfix(
                ItemSpecificationPanel __instance,
                Item ___item_0,
                CompactCharacteristicPanel ____compactCharTemplate,
                Transform ____compactPanel,
                SimpleTooltip ___simpleTooltip_0)
            {
                if (!Settings.ShowModStats.Value || !(___item_0 is Mod))
                {
                    return;
                }

                bool changed;
                var deepAttributes = GetDeepAttributes(___item_0, out changed);
                if (!changed)
                {
                    return;
                }

                var compactPanels = AttributeCompactPanelDictionaryField.GetValue(__instance) as IDisposable;
                // Clean up existing one
                if (compactPanels != null)
                {
                    compactPanels.Dispose();
                }

                var newCompactPanels = Activator.CreateInstance(AttributeCompactPanelDictionaryField.FieldType,
                [
                    deepAttributes,
                    ____compactCharTemplate,
                    ____compactPanel,
                    (ItemAttributeClass attribute, CompactCharacteristicPanel viewer) => viewer.Show(attribute, ___simpleTooltip_0, __instance.Boolean_0, 100)
                ]) as IEnumerable<KeyValuePair<ItemAttributeClass, CompactCharacteristicPanel>>;

                AttributeCompactPanelDictionaryField.SetValue(__instance, newCompactPanels);

                if (newCompactPanels.Any())
                {
                    newCompactPanels.Last().Value.OnTextWidthCalculated += __instance.method_3;
                    int siblingIndex = newCompactPanels.Last().Value.Transform.GetSiblingIndex();

                    var compactDropdownPanels = AttributeCompactDropdownDictionaryField.GetValue(__instance) as IEnumerable<KeyValuePair<ItemAttributeClass, CompactCharacteristicDropdownPanel>>;
                    foreach (var item in compactDropdownPanels)
                    {
                        item.Value.Transform.SetSiblingIndex(++siblingIndex);
                    }
                }
                __instance.method_10(0f);
                __instance.method_6(null);
            }
        }

        // The fundamental thing about mods is that unlike weapons, armor, etc, they do not change their own attributes when they "accept" inner mods.
        // I guess weapons figure out their stats by deeply iterating all mods, rather than just their direct mods
        // As a result, the compare method that works with weapons/armor doesn't work with mods. Normally, it "adds" the mod, clones the result, then reverts the "add". Hence
        // the compareItem is the item with the mods. But again, as mods don't change their values, we see no change.
        // I wish I could prefix method_6 and update the compare item with the deep attributes, but that only works when adding a mod
        // When removing, current item and compare item end up the same since current item never considers the mod anyway
        // So I have to forcably call the refresh values method
        private class CompareModPatch : ModulePatch
        {
            private static MethodInfo RefreshStaticMethod;

            protected override MethodBase GetTargetMethod()
            {
                RefreshStaticMethod = AccessTools.Method(typeof(ItemSpecificationPanel), "smethod_1", null, [typeof(CompactCharacteristicPanel)]);

                return AccessTools.Method(typeof(ItemSpecificationPanel), "method_6");
            }

            [PatchPrefix]
            private static void Prefix(Item compareItem)
            {
                if (compareItem is ArmorClass)
                {
                    // Armor points is added in method_5, but not in other places so it's missed by compare
                    ArmorComponent[] armorComponents = compareItem.GetItemComponentsInChildren<ArmorComponent>(true).Where(c => c.ArmorClass > 0).ToArray<ArmorComponent>();
                    float maxDurability = armorComponents.Sum(c => c.Repairable.Durability);

                    ItemAttributeClass itemAttributeClass = new ItemAttributeClass(EItemAttributeId.ArmorPoints);
                    itemAttributeClass.Name = EItemAttributeId.ArmorPoints.GetName();
                    itemAttributeClass.Base = () => maxDurability;
                    itemAttributeClass.StringValue = () => Math.Round(maxDurability, 1).ToString(CultureInfo.InvariantCulture);
                    itemAttributeClass.DisplayType = () => EItemAttributeDisplayType.Compact;

                    compareItem.Attributes.Insert(0, itemAttributeClass);
                }
            }

            [PatchPostfix]
            private static void Postfix(ItemSpecificationPanel __instance, Item compareItem)
            {
                if (!Settings.ShowModStats.Value || !(compareItem is Mod))
                {
                    return;
                }

                bool changed;
                List<ItemAttributeClass> deepAttributes = GetDeepAttributes(compareItem, out changed);
                if (!changed)
                {
                    return;
                }

                var compactPanels = AttributeCompactPanelDictionaryField.GetValue(__instance);
                RefreshStaticMethod.Invoke(null, [compactPanels, deepAttributes]);
            }
        }

        private class InjectButtonPatch : ModulePatch
        {
            private static FieldInfo InteractionsButtonsContainerButtonTemplateField;
            private static FieldInfo InteractionsButtonsContainerContainerField;

            private static FieldInfo InteractionsButtonContainerUIField;
            private static MethodInfo InteractionsButtonContainerUIDisposeMethod;

            private static FieldInfo SimpleContextMenuButtonTextField;

            protected override MethodBase GetTargetMethod()
            {
                InteractionsButtonsContainerButtonTemplateField = AccessTools.Field(typeof(InteractionButtonsContainer), "_buttonTemplate");
                InteractionsButtonsContainerContainerField = AccessTools.Field(typeof(InteractionButtonsContainer), "_buttonsContainer");

                InteractionsButtonContainerUIField = AccessTools.Field(typeof(InteractionButtonsContainer), "UI");
                InteractionsButtonContainerUIDisposeMethod = AccessTools.Method(InteractionsButtonContainerUIField.FieldType, "AddDisposable", [typeof(Action)]);

                SimpleContextMenuButtonTextField = AccessTools.Field(typeof(ContextMenuButton), "_text");

                return AccessTools.Method(typeof(ItemSpecificationPanel), "method_4");
            }

            private static string GetLabel()
            {
                return Settings.ShowModStats.Value ? "HIDE MOD STATS" : "SHOW MOD STATS";
            }

            [PatchPostfix]
            private static void Postfix(ItemSpecificationPanel __instance, ItemInfoInteractionsAbstractClass<EItemInfoButton> contextInteractions, Item ___item_0, InteractionButtonsContainer ____interactionButtonsContainer)
            {
                if (!(___item_0 is Mod))
                {
                    return;
                }

                SimpleContextMenuButton template = InteractionsButtonsContainerButtonTemplateField.GetValue(____interactionButtonsContainer) as SimpleContextMenuButton;
                Transform transform = InteractionsButtonsContainerContainerField.GetValue(____interactionButtonsContainer) as Transform;

                SimpleContextMenuButton toggleButton = null;

                Action onClick = () =>
                {
                    Settings.ShowModStats.Value = !Settings.ShowModStats.Value;

                    var text = SimpleContextMenuButtonTextField.GetValue(toggleButton) as TextMeshProUGUI;
                    text.text = GetLabel();

                    __instance.method_5(); // rebuild stat panels
                };

                // Listen to the setting to handle multiple windows open at once
                EventHandler onSettingChanged = (sender, args) =>
                {
                    var text = SimpleContextMenuButtonTextField.GetValue(toggleButton) as TextMeshProUGUI;
                    text.text = GetLabel();
                };
                Settings.ShowModStats.SettingChanged += onSettingChanged;

                Action createButton = () => 
                {
                    Sprite sprite = CacheResourcesPopAbstractClass.Pop<Sprite>("Characteristics/Icons/Modding");
                    toggleButton = UnityEngine.Object.Instantiate(template, transform, false);
                    toggleButton.Show(GetLabel(), null, sprite, onClick, null);
                    ____interactionButtonsContainer.method_5(toggleButton); // add to disposable list
                };

                // Subscribe to redraws to recreate when mods get dropped in
                contextInteractions.OnRedrawRequired += createButton;

                // And unsubscribe when the window goes away
                InteractionsButtonContainerUIDisposeMethod.Invoke(InteractionsButtonContainerUIField.GetValue(____interactionButtonsContainer), [() => 
                {
                    contextInteractions.OnRedrawRequired -= createButton;
                    Settings.ShowModStats.SettingChanged -= onSettingChanged;
                }]);

                createButton();
            }
        }

        private class FormatValuesPatch : ModulePatch
        {
            // These fields are percents, but have been manually multipied by 100 already
            private static EItemAttributeId[] NonPercentPercents = [EItemAttributeId.ChangeMovementSpeed, EItemAttributeId.ChangeTurningSpeed, EItemAttributeId.Ergonomics];

            private static FieldInfo ItemAttributeField;
            private static FieldInfo IncreasingColorField;
            private static FieldInfo DecreasingColorField;

            protected override MethodBase GetTargetMethod()
            {
                ItemAttributeField = AccessTools.Field(typeof(CompactCharacteristicPanel), "ItemAttribute");
                IncreasingColorField = AccessTools.Field(typeof(CompactCharacteristicPanel), "_increasingColor");
                DecreasingColorField = AccessTools.Field(typeof(CompactCharacteristicPanel), "_decreasingColor");

                return AccessTools.Method(typeof(CompactCharacteristicPanel), "SetValues");
            }

            [PatchPostfix]
            private static void Postfix(CompactCharacteristicPanel __instance, TextMeshProUGUI ___ValueText)
            {
                // Comparisons are shown as <value>(<changed>)
                // <value> is from each attribute type's StringValue() function, so is formatted *mostly* ok
                // <changed> is just naively formatted with ToString("F2"), so I have to figure out what it is and fix that
                // This method is a gnarly pile of regex and replacements, blame BSG

                Color increasingColor = (Color)IncreasingColorField.GetValue(__instance);
                string increasingColorHex = "#" + ColorUtility.ToHtmlStringRGB(increasingColor);

                Color decreasingColor = (Color)DecreasingColorField.GetValue(__instance);
                string decreasingColorHex = "#" + ColorUtility.ToHtmlStringRGB(decreasingColor);

                string text = ___ValueText.text;
                ItemAttributeClass attribute = ItemAttributeField.GetValue(__instance) as ItemAttributeClass;

                // Some percents are formatted with ToString("P1"), which puts a space before the %. These are percents from 0-1, so the <changed> value need to be converted
                var match = Regex.Match(text, @" %\((.*)\)");
                if (match.Success)
                {
                    float value = float.Parse(match.Groups[1].Value);
                    string sign = value > 0 ? "+" : "";
                    string color = (attribute.LessIsGood && value < 0) || (!attribute.LessIsGood && value > 0) ? increasingColorHex : decreasingColorHex;

                    // Except some that have a space weren't actually formatted with P1 and are 0-100 with a manually added " %"
                    if (NonPercentPercents.Contains((EItemAttributeId)attribute.Id))
                    {
                        text = Regex.Replace(text, @"%\(.*\)", "%<color=" + color + ">(" + sign + value + "%)</color>");
                    }
                    else
                    {
                        text = Regex.Replace(text, @"%\(.*\)", "%<color=" + color + ">(" + sign + value.ToString("P1") + ")</color>");
                    }
                }
                else
                {
                    // Others are rendered as num + "%", so there's no space before the %. These are percents but are from 0-100, not 0-1.
                    match = Regex.Match(text, @"(\S)%\((.*)\)");
                    if (match.Success)
                    {
                        float value = float.Parse(match.Groups[2].Value);
                        string sign = value > 0 ? "+" : "";
                        string color = (attribute.LessIsGood && value < 0) || (!attribute.LessIsGood && value > 0) ? increasingColorHex : decreasingColorHex;
                        text = Regex.Replace(text, @"(\S)%\((.*)\)", match.Groups[1].Value + "%<color=" + color + ">(" + sign + value + "%)</color>");
                    }
                    else
                    {
                        // Finally the ones that aren't percents
                        match = Regex.Match(text, @"\((.*)\)");
                        if (match.Success)
                        {
                            float value = float.Parse(match.Groups[1].Value);
                            string sign = value > 0 ? "+" : "";
                            string color = (attribute.LessIsGood && value < 0) || (!attribute.LessIsGood && value > 0) ? increasingColorHex : decreasingColorHex;
                            text = Regex.Replace(text, @"\((.*)\)", "<color=" + color + ">(" + sign + value + ")</color>");
                        }
                    }
                }

                // Remove trailing 0s
                text = Regex.Replace(text, @"\.([1-9]*)0+\b", ".$1");
                text = Regex.Replace(text, @"\.\B", "");

                // Fix spacing
                text = text.Replace(" %", "%");
                text = text.Replace("(", " (");

                ___ValueText.text = text;
            }
        }

        private static List<ItemAttributeClass> GetDeepAttributes(Item item, out bool changed)
        {
            changed = false;
            List<ItemAttributeClass> itemAttributes = item.Attributes.Where(a => a.DisplayType() == EItemAttributeDisplayType.Compact).ToList();
            var subComponents = item.GetItemComponentsInChildren<IItemComponent>(true);
            foreach (var subComponent in subComponents)
            {
                var subItem = ItemComponentItemField.GetValue(subComponent) as Item;
                if (subItem == item)
                {
                    continue;
                }

                var subAttributes = GetDeepAttributes(subItem, out changed);
                itemAttributes = CombineAttributes(itemAttributes, subAttributes).ToList();
                changed = true;
            }

            return itemAttributes;
        }

        private static IEnumerable<ItemAttributeClass> CombineAttributes(IList<ItemAttributeClass> first, IList<ItemAttributeClass> second)
        {
            foreach (EItemAttributeId id in first.Select(a => a.Id).Union(second.Select(a => a.Id)))
            {
                // Need to cast the id since it's of type Enum for some reason
                var attribute = first.FirstOrDefault(a => (EItemAttributeId)a.Id == id);
                var other = second.FirstOrDefault(a => (EItemAttributeId)a.Id == id);
                if (attribute == null)
                {
                    yield return other;
                }
                else if (other == null)
                {
                    yield return attribute;
                }
                else
                {
                    var combined = attribute.Clone();
                    switch (attribute.Id)
                    {
                        case EItemAttributeId.EffectiveDist:
                        case EItemAttributeId.SightingRange:
                            combined.Base = () => Math.Max(attribute.Base(), other.Base());
                            combined.StringValue = () => combined.Base().ToString();
                            break;
                        case EItemAttributeId.Accuracy:
                        case EItemAttributeId.Recoil:
                            combined.Base = () => attribute.Base() + other.Base();
                            combined.StringValue = () => combined.Base() + "%";
                            break;
                        case EItemAttributeId.Loudness:
                        case EItemAttributeId.Ergonomics:
                        case EItemAttributeId.Velocity:
                            combined.Base = () => attribute.Base() + other.Base();
                            combined.StringValue = () => combined.Base().ToString();
                            break;
                        case EItemAttributeId.DurabilityBurn:
                        case EItemAttributeId.HeatFactor:
                        case EItemAttributeId.CoolFactor:
                            combined.Base = () => attribute.Base() + other.Base();
                            combined.StringValue = () => combined.Base().ToString("P1");
                            break;
                        case EItemAttributeId.RaidModdable:
                            break;
                        default:
                            break;
                    }

                    yield return combined;
                }
            }
        }
    }
}
