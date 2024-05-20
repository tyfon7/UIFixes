using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace UIFixes
{
    public class ItemPanelPatches
    {
        private static FieldInfo AttributeCompactPanelDictionaryField;
        private static FieldInfo AttributeCompactDropdownDictionaryField;

        private static FieldInfo CompactCharacteristicPanelItemAttributeField;
        private static FieldInfo CompactCharacteristicPanelCompareItemAttributeField;

        public static void Enable()
        {
            AttributeCompactPanelDictionaryField = AccessTools.GetDeclaredFields(typeof(ItemSpecificationPanel)).First(f => typeof(IEnumerable<KeyValuePair<ItemAttributeClass, CompactCharacteristicPanel>>).IsAssignableFrom(f.FieldType));
            AttributeCompactDropdownDictionaryField = AccessTools.GetDeclaredFields(typeof(ItemSpecificationPanel)).First(f => typeof(IEnumerable<KeyValuePair<ItemAttributeClass, CompactCharacteristicDropdownPanel>>).IsAssignableFrom(f.FieldType));

            CompactCharacteristicPanelItemAttributeField = AccessTools.Field(typeof(CompactCharacteristicPanel), "ItemAttribute");
            CompactCharacteristicPanelCompareItemAttributeField = AccessTools.Field(typeof(CompactCharacteristicPanel), "CompareItemAttribute");

            new InjectButtonPatch().Enable();
            new LoadModStatsPatch().Enable();
            new CompareModPatch().Enable();
            new FormatCompactValuesPatch().Enable();
            new FormatFullValuesPatch().Enable();
        }

        private class LoadModStatsPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ItemSpecificationPanel), nameof(ItemSpecificationPanel.method_5));
            }

            [PatchPostfix]
            public static void Postfix(
                ItemSpecificationPanel __instance,
                Item ___item_0,
                CompactCharacteristicPanel ____compactCharTemplate,
                Transform ____compactPanel,
                SimpleTooltip ___simpleTooltip_0)
            {
                if (!Settings.ShowModStats.Value || ___item_0 is not Mod)
                {
                    return;
                }

                var deepAttributes = GetDeepAttributes(___item_0, out bool changed);
                if (!changed)
                {
                    return;
                }

                // Clean up existing one
                if (AttributeCompactPanelDictionaryField.GetValue(__instance) is IDisposable compactPanels)
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
                RefreshStaticMethod = AccessTools.Method(typeof(ItemSpecificationPanel), nameof(ItemSpecificationPanel.smethod_1), null, [typeof(CompactCharacteristicPanel)]);

                return AccessTools.Method(typeof(ItemSpecificationPanel), nameof(ItemSpecificationPanel.method_6));
            }

            [PatchPrefix]
            public static void Prefix(Item compareItem)
            {
                if (compareItem == null)
                {
                    return;
                }

                // Armor points is added in method_5, but not in other places so it's missed by compare
                ArmorComponent[] armorComponents = compareItem.GetItemComponentsInChildren<ArmorComponent>(true).Where(c => c.ArmorClass > 0).ToArray<ArmorComponent>();
                if (armorComponents.Any())
                {
                    float maxDurability = armorComponents.Sum(c => c.Repairable.Durability);

                    var itemAttributeClass = new ItemAttributeClass(EItemAttributeId.ArmorPoints)
                    {
                        Name = EItemAttributeId.ArmorPoints.GetName(),
                        Base = () => maxDurability,
                        StringValue = () => Math.Round(maxDurability, 1).ToString(CultureInfo.InvariantCulture),
                        DisplayType = () => EItemAttributeDisplayType.Compact
                    };

                    compareItem.Attributes.Insert(0, itemAttributeClass);
                }
            }

            [PatchPostfix]
            public static void Postfix(ItemSpecificationPanel __instance, Item compareItem)
            {
                if (!Settings.ShowModStats.Value || compareItem is not Mod)
                {
                    return;
                }

                List<ItemAttributeClass> deepAttributes = GetDeepAttributes(compareItem, out bool changed);
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
            private static MethodInfo InteractionsButtonContainerUIAddDisposableMethod;

            private static FieldInfo SimpleContextMenuButtonTextField;

            protected override MethodBase GetTargetMethod()
            {
                InteractionsButtonsContainerButtonTemplateField = AccessTools.Field(typeof(InteractionButtonsContainer), "_buttonTemplate");
                InteractionsButtonsContainerContainerField = AccessTools.Field(typeof(InteractionButtonsContainer), "_buttonsContainer");

                InteractionsButtonContainerUIField = AccessTools.Field(typeof(InteractionButtonsContainer), "UI");
                InteractionsButtonContainerUIAddDisposableMethod = AccessTools.Method(InteractionsButtonContainerUIField.FieldType, "AddDisposable", [typeof(Action)]);

                SimpleContextMenuButtonTextField = AccessTools.Field(typeof(ContextMenuButton), "_text");

                return AccessTools.Method(typeof(ItemSpecificationPanel), nameof(ItemSpecificationPanel.method_4));
            }

            private static string GetLabel()
            {
                return Settings.ShowModStats.Value ? "HIDE MOD STATS" : "SHOW MOD STATS";
            }

            [PatchPostfix]
            public static void Postfix(ItemSpecificationPanel __instance, ItemInfoInteractionsAbstractClass<EItemInfoButton> contextInteractions, Item ___item_0, InteractionButtonsContainer ____interactionButtonsContainer)
            {
                if (___item_0 is not Mod)
                {
                    return;
                }

                SimpleContextMenuButton template = InteractionsButtonsContainerButtonTemplateField.GetValue(____interactionButtonsContainer) as SimpleContextMenuButton;
                Transform transform = InteractionsButtonsContainerContainerField.GetValue(____interactionButtonsContainer) as Transform;

                SimpleContextMenuButton toggleButton = null;

                // Listen to the setting and the work there to handle multiple windows open at once
                void onSettingChanged(object sender, EventArgs args)
                {
                    var text = SimpleContextMenuButtonTextField.GetValue(toggleButton) as TextMeshProUGUI;
                    text.text = GetLabel();

                    __instance.method_5(); // rebuild stat panels
                }
                Settings.ShowModStats.SettingChanged += onSettingChanged;

                static void onClick()
                {
                    Settings.ShowModStats.Value = !Settings.ShowModStats.Value;
                }

                void createButton()
                {
                    Sprite sprite = CacheResourcesPopAbstractClass.Pop<Sprite>("Characteristics/Icons/Modding");
                    toggleButton = UnityEngine.Object.Instantiate(template, transform, false);
                    toggleButton.Show(GetLabel(), null, sprite, onClick, null);
                    ____interactionButtonsContainer.method_5(toggleButton); // add to disposable list
                }

                // Subscribe to redraws to recreate when mods get dropped in
                contextInteractions.OnRedrawRequired += createButton;

                // And unsubscribe when the window goes away
                InteractionsButtonContainerUIAddDisposableMethod.Invoke(InteractionsButtonContainerUIField.GetValue(____interactionButtonsContainer), [() =>
                {
                    contextInteractions.OnRedrawRequired -= createButton;
                    Settings.ShowModStats.SettingChanged -= onSettingChanged;
                }]);

                createButton();
            }
        }

        private class FormatCompactValuesPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(CompactCharacteristicPanel), nameof(CompactCharacteristicPanel.SetValues));
            }

            [PatchPostfix]
            public static void Postfix(CompactCharacteristicPanel __instance, TextMeshProUGUI ___ValueText)
            {
                try
                {
                    FormatText(__instance, ___ValueText);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }
        }

        private class FormatFullValuesPatch : ModulePatch
        {
            private static MethodInfo RoundToIntMethod;
            private static MethodInfo ToStringMethod;

            protected override MethodBase GetTargetMethod()
            {
                RoundToIntMethod = AccessTools.Method(typeof(Mathf), nameof(Mathf.RoundToInt));
                ToStringMethod = AccessTools.Method(typeof(float), nameof(float.ToString), [typeof(string)]);

                return AccessTools.Method(typeof(CharacteristicPanel), nameof(CharacteristicPanel.SetValues));
            }

            [PatchPostfix]
            public static void Postfix(CharacteristicPanel __instance, TextMeshProUGUI ___ValueText)
            {
                try
                {
                    FormatText(__instance, ___ValueText, true);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                }
            }

            // This transpiler looks for where it rounds a float to an int, and skips that. Instead it calls ToString("0.0#") on it
            [PatchTranspiler]
            public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
            {
                int skip = 0;
                CodeInstruction lastInstruction = null;
                CodeInstruction currentInstruction = null;
                foreach (var instruction in instructions)
                {
                    if (lastInstruction == null)
                    {
                        lastInstruction = instruction;
                        continue;
                    }

                    currentInstruction = instruction;

                    if (skip > 0)
                    {
                        --skip;
                    }
                    else if (currentInstruction.Calls(RoundToIntMethod))
                    {
                        yield return new CodeInstruction(OpCodes.Ldloca_S, 17);
                        yield return new CodeInstruction(OpCodes.Ldstr, "0.0#");
                        yield return new CodeInstruction(OpCodes.Call, ToStringMethod);
                        skip = 4;
                    }
                    else
                    {
                        yield return lastInstruction;
                    }

                    lastInstruction = instruction;
                }

                if (currentInstruction != null)
                {
                    yield return currentInstruction;
                }
            }
        }

        // These fields are percents, but have been manually multipied by 100 already
        private static readonly EItemAttributeId[] NonPercentPercents = [EItemAttributeId.ChangeMovementSpeed, EItemAttributeId.ChangeTurningSpeed, EItemAttributeId.Ergonomics];

        private static void FormatText(CompactCharacteristicPanel panel, TextMeshProUGUI textMesh, bool fullBar = false)
        {
            // Comparisons are shown as <value>(<changed>)
            // <value> is from each attribute type's StringValue() function, so is formatted *mostly* ok
            // <changed> is just naively formatted with ToString("F2"), so I have to figure out what it is and fix that
            // This method is a gnarly pile of regex and replacements, blame BSG
            if (!Settings.StyleItemPanel.Value)
            {
                return;
            }

            // These come from CompactCharacteristicPanel._increasingColor and _decreasingColor, which are hardcoded. Hardcoding here too because 
            // CharacteristicPanel doesn't define and you get clear
            const string IncreasingColorHex = "#5EC1FF";
            const string DecreasingColorHex = "#C40000";

            string text = textMesh.text;
            ItemAttributeClass attribute = CompactCharacteristicPanelItemAttributeField.GetValue(panel) as ItemAttributeClass;

            // Holy shit did they mess up MOA. Half of the calculation is done in the StringValue() method, so calculating delta from Base() loses all that
            // Plus, they round the difference to the nearest integer (!?)
            // Completely redo it
            if ((EItemAttributeId)attribute.Id == EItemAttributeId.CenterOfImpact)
            {
                if (CompactCharacteristicPanelCompareItemAttributeField.GetValue(panel) is ItemAttributeClass compareAttribute)
                {
                    string currentStringValue = attribute.StringValue();
                    var moaMatch = Regex.Match(currentStringValue, @"^(\S+)");
                    if (float.TryParse(moaMatch.Groups[1].Value, out float moa))
                    {
                        string compareStringValue = compareAttribute.StringValue();
                        moaMatch = Regex.Match(compareStringValue, @"^(\S+)");
                        if (float.TryParse(moaMatch.Groups[1].Value, out float compareMoa))
                        {
                            float delta = compareMoa - moa;
                            string final = currentStringValue;
                            if (Math.Abs(delta) > 0)
                            {
                                string sign = delta > 0 ? "+" : "";
                                string color = (attribute.LessIsGood && delta < 0) || (!attribute.LessIsGood && delta > 0) ? IncreasingColorHex : DecreasingColorHex;
                                final += " <color=" + color + ">(" + sign + delta.ToString("0.0#") + ")</color>";
                            }

                            textMesh.text = final;
                            return;
                        }
                    }
                }
            }

            // Some percents are formatted with ToString("P1"), which puts a space before the %. These are percents from 0-1, so the <changed> value need to be converted
            var match = Regex.Match(text, @" %\(([+-].*)\)");
            if (match.Success)
            {
                // If this fails to parse, I don't know what it is, leave it be
                if (float.TryParse(match.Groups[1].Value, out float value))
                {
                    string sign = value > 0 ? "+" : "";
                    string color = (attribute.LessIsGood && value < 0) || (!attribute.LessIsGood && value > 0) ? IncreasingColorHex : DecreasingColorHex;

                    // Except some that have a space weren't actually formatted with P1 and are 0-100 with a manually added " %"
                    if (NonPercentPercents.Contains((EItemAttributeId)attribute.Id))
                    {
                        text = Regex.Replace(text, @"%\([+-].*\)", "%<color=" + color + ">(" + sign + value + "%)</color>");
                    }
                    else
                    {
                        text = Regex.Replace(text, @"%\([+-].*\)", "%<color=" + color + ">(" + sign + value.ToString("P1") + ")</color>");
                    }
                }
            }
            else
            {
                // Others are rendered as num + "%", so there's no space before the %. These are percents but are from 0-100, not 0-1.
                match = Regex.Match(text, @"(\S)%\(([+-].*)\)");
                if (match.Success)
                {
                    // If this fails to parse, I don't know what it is, leave it be
                    if (float.TryParse(match.Groups[2].Value, out float value))
                    {
                        string sign = value > 0 ? "+" : "";
                        string color = (attribute.LessIsGood && value < 0) || (!attribute.LessIsGood && value > 0) ? IncreasingColorHex : DecreasingColorHex;
                        text = Regex.Replace(text, @"(\S)%\(([+-].*)\)", match.Groups[1].Value + "%<color=" + color + ">(" + sign + value + "%)</color>");
                    }
                }
                else
                {
                    // Finally the ones that aren't percents
                    match = Regex.Match(text, @"\(([+-].*)\)");
                    if (match.Success)
                    {
                        // If this fails to parse, I don't know what it is, leave it be
                        if (float.TryParse(match.Groups[1].Value, out float value))
                        {
                            string sign = value > 0 ? "+" : "";
                            string color = (attribute.LessIsGood && value < 0) || (!attribute.LessIsGood && value > 0) ? IncreasingColorHex : DecreasingColorHex;
                            if (fullBar && Math.Abs(value) >= 1)
                            {
                                // Fullbar rounds to nearest int, but I transpiled it not to. Restore the rounding, but only if the value won't just round to 0
                                value = Mathf.RoundToInt(value);
                            }
                            text = Regex.Replace(text, @"\(([+-].*)\)", "<color=" + color + ">(" + sign + value + ")</color>");
                        }
                    }
                }
            }

            // Remove trailing 0s
            text = RemoveTrailingZeros(text);

            // Fix spacing
            text = text.Replace(" %", "%");
            text = text.Replace("(", " (");

            textMesh.text = text;
        }

        private static List<ItemAttributeClass> GetDeepAttributes(Item item, out bool changed)
        {
            changed = false;
            List<ItemAttributeClass> itemAttributes = item.Attributes.Where(a => a.DisplayType() == EItemAttributeDisplayType.Compact).ToList();
            foreach (var subItem in item.GetAllItems()) // This get all items, recursively
            {
                if (subItem == item)
                {
                    continue;
                }

                var subAttributes = subItem.Attributes.Where(a => a.DisplayType() == EItemAttributeDisplayType.Compact).ToList();
                itemAttributes = CombineAttributes(itemAttributes, subAttributes).ToList();
                changed = true;
            }

            return itemAttributes;
        }

        private static IEnumerable<ItemAttributeClass> CombineAttributes(IList<ItemAttributeClass> first, IList<ItemAttributeClass> second)
        {
            foreach (EItemAttributeId id in first.Select(a => a.Id).Union(second.Select(a => a.Id)).Select(v => (EItemAttributeId)v))
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

        public static string RemoveTrailingZeros(string input)
        {
            // This matches: a number (so it doesn't apply to periods in words), named "integer"
            // Followed by either
            // a) a dot, some digits, and then a non-zero digit (named "significantDigits"), which is followed by one or more trailing 0
            // b) a dot and some trailing 0
            // And all that is replaced to the original integer, and the significantDigits (if they exist)
            // If neither matches this doesn't match and does nothing
            return Regex.Replace(input, @"(?<integer>\d)((?<significantDecimals>\.[0-9]*[1-9])0*\b)?(\.0+\b)?", "${integer}${significantDecimals}");
        }
    }
}
