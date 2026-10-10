using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.Utilities;
using HarmonyLib;
using SPTushonka.Reflection.Patching;
using TMPro;
using UnityEngine;

namespace UIFixes;

public static class InspectWindowStatsPatches
{
    public static void Enable()
    {
        new CalculateModStatsPatch().Enable();
        new CompareModStatsPatch().Enable();
        new AddShowHideModStatsButtonPatch().Enable();
        new FormatCompactValuesPatch().Enable();
        new FormatFullValuesPatch().Enable();
        new FixDurabilityBarPatch().Enable();

        new HighlightSlotsPatch().Enable();
        new HighlightFilledSlotsPatch().Enable();

        new FixTraderCompatWithPatch().Enable();
    }

    public class CalculateModStatsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemSpecificationPanel), nameof(ItemSpecificationPanel.RecreateAttributeBars));
        }

        [PatchPostfix]
        public static void Postfix(ItemSpecificationPanel __instance)
        {
            if (!Settings.ShowModStats.Value || __instance._item is not Mod)
            {
                return;
            }

            var deepAttributes = GetDeepAttributes(__instance._item, out bool changed);
            if (!changed)
            {
                return;
            }

            // Clean up existing one
            if (__instance._compactBarAttributeViewers is IDisposable compactPanels)
            {
                compactPanels.Dispose();
            }

            // Need to explicitly declare the action so the type information is there for conversion to Il2CppSystem.Action below
            Action<ItemAttribute, CompactCharacteristicPanel> callback = (attribute, viewer) => viewer.Show(attribute, __instance._secondaryTooltip, __instance.Examined, 100);

            __instance._compactBarAttributeViewers = new ViewList<ItemAttribute, CompactCharacteristicPanel>(
                deepAttributes,
                __instance._compactCharTemplate,
                __instance._compactPanel,
                callback);

            if (__instance._compactBarAttributeViewers.Any())
            {
                __instance._compactBarAttributeViewers.Last().Value.OnTextWidthCalculated += __instance.CompactItemTextCalculated;
                int siblingIndex = __instance._compactBarAttributeViewers.Last().Value.Transform.GetSiblingIndex();

                foreach (var item in __instance._createdCompatibleAttrivieViewers)
                {
                    item.Value.Transform.SetSiblingIndex(++siblingIndex);
                }
            }

            __instance.ProcessCompactCells(0f);
            __instance.Compare(null);
        }
    }

    // The fundamental thing about mods is that unlike weapons, armor, etc, they do not change their own attributes when they "accept" inner mods.
    // I guess weapons figure out their stats by deeply iterating all mods, rather than just their direct mods
    // As a result, the compare method that works with weapons/armor doesn't work with mods. Normally, it "adds" the mod, clones the result, then reverts the "add". Hence
    // the compareItem is the item with the mods. But again, as mods don't change their values, we see no change.
    // I wish I could prefix Compare() and update the compare item with the deep attributes, but that only works when adding a mod
    // When removing, current item and compare item end up the same since current item never considers the mod anyway
    // So I have to forcably call the refresh values method
    public class CompareModStatsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemSpecificationPanel), nameof(ItemSpecificationPanel.Compare));
        }

        [PatchPrefix]
        public static void Prefix(Item compareItem)
        {
            if (compareItem == null)
            {
                return;
            }

            // Armor points is added in RecreateAttributeBars, but not in other places so it's missed by compare
            var armorComponents = compareItem.GetItemComponentsInChildren<ArmorComponent>(true).Where(c => c.ArmorClass > 0).ToArray<ArmorComponent>();
            if (armorComponents.Any())
            {
                float maxDurability = armorComponents.Sum(c => c.Repairable.Durability);

                Func<float> getBase = () => maxDurability;
                Func<string> getString = () => Math.Round(maxDurability, 1).ToString(CultureInfo.InvariantCulture);
                Func<EItemAttributeDisplayType> getDisplay = () => EItemAttributeDisplayType.Compact;

                var itemAttributeClass = new ItemAttribute(EItemAttributeId.ArmorPoints.ToIl2CppEnum())
                {
                    Name = EItemAttributeId.ArmorPoints.GetName(),
                    Base = getBase,
                    StringValue = getString,
                    DisplayType = getDisplay
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

            var deepAttributes = GetDeepAttributes(compareItem, out bool changed);
            if (!changed)
            {
                return;
            }

            // Do this manually to update, no longer have access to the code that would do this
            foreach (var (attribute, panel) in __instance._compactBarAttributeViewers)
            {
                var changedAttribute = deepAttributes.FirstOrDefault(a => a.Id.Equals(attribute.Id));
                panel.CompareWith(changedAttribute);
            }
        }
    }

    public class AddShowHideModStatsButtonPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemSpecificationPanel), nameof(ItemSpecificationPanel.InitInteractionButtonsPanel));
        }

        private static string GetLabel()
        {
            return Settings.ShowModStats.Value ? "HIDE MOD STATS" : "SHOW MOD STATS";
        }

        [PatchPostfix]
        public static void Postfix(ItemSpecificationPanel __instance, ContextInteractions<EItemInfoButton> contextInteractions)
        {
            if (__instance._item is not Mod)
            {
                return;
            }

            var buttonsContainer = __instance._interactionButtonsContainer;

            ContextMenuButton toggleButton = null;

            // Listen to the setting and the work there to handle multiple windows open at once
            void OnSettingChanged(object sender, EventArgs args)
            {
                var text = toggleButton._text;
                text.text = GetLabel();

                __instance.RecreateAttributeBars(); // rebuild stat panels
            }
            Settings.ShowModStats.SettingChanged += OnSettingChanged;

            Action onClick = () => Settings.ShowModStats.Value = !Settings.ShowModStats.Value;

            void CreateButton()
            {
                Sprite sprite = ResourcesCache.Pop<Sprite>("Characteristics/Icons/Modding");
                toggleButton = (ContextMenuButton)UnityEngine.Object.Instantiate(buttonsContainer._buttonTemplate, buttonsContainer._buttonsContainer, false);
                toggleButton.Show(GetLabel(), null, sprite, onClick, null);
                toggleButton.transform.SetSiblingIndex(toggleButton.transform.GetSiblingIndex() - 1);
                buttonsContainer.BindButton(toggleButton); // add to disposable list
            }

            // Subscribe to redraws to recreate when mods get dropped in
            contextInteractions.OnRedrawRequired += CreateButton;

            // And unsubscribe when the window goes away
            buttonsContainer.UI.AddOnDispose(() =>
            {
                contextInteractions.OnRedrawRequired -= CreateButton;
                Settings.ShowModStats.SettingChanged -= OnSettingChanged;
            });

            CreateButton();
        }
    }

    public class FormatCompactValuesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(CompactCharacteristicPanel), nameof(CompactCharacteristicPanel.SetValues));
        }

        [PatchPostfix]
        public static void Postfix(CompactCharacteristicPanel __instance)
        {
            try
            {
                FormatText(__instance, __instance.ValueText);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }

    public class FormatFullValuesPatch : ModulePatch
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
        public static void Postfix(CharacteristicPanel __instance)
        {
            try
            {
                FormatText(__instance, __instance.ValueText, true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        // 5.0: RIP to transpiler methods
        // This transpiler looks for where it rounds a float to an int, and skips that. Instead it calls ToString("0.0#") on it
        // [PatchTranspiler]
        // public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        // {
        //     int skip = 0;
        //     CodeInstruction lastInstruction = null;
        //     CodeInstruction currentInstruction = null;
        //     foreach (var instruction in instructions)
        //     {
        //         if (lastInstruction == null)
        //         {
        //             lastInstruction = instruction;
        //             continue;
        //         }

        //         currentInstruction = instruction;

        //         if (skip > 0)
        //         {
        //             --skip;
        //         }
        //         else if (currentInstruction.Calls(RoundToIntMethod))
        //         {
        //             yield return new CodeInstruction(OpCodes.Ldloca_S, 17);
        //             yield return new CodeInstruction(OpCodes.Ldstr, "0.0#");
        //             yield return new CodeInstruction(OpCodes.Call, ToStringMethod);
        //             skip = 4;
        //         }
        //         else
        //         {
        //             yield return lastInstruction;
        //         }

        //         lastInstruction = instruction;
        //     }

        //     if (currentInstruction != null)
        //     {
        //         yield return currentInstruction;
        //     }
        // }
    }


    // Bar width is currently set to durability/100, and that 100 is pretty much hardcoded by the client
    // Just clamp the bar to keep it from overflowing
    public class FixDurabilityBarPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(DurabilityPanel), nameof(DurabilityPanel.SetValues));
        }
        [PatchPostfix]
        public static void Postfix(DurabilityPanel __instance)
        {
            __instance.Current.rectTransform.anchorMax = new Vector2(
                Mathf.Min(__instance.Current.rectTransform.anchorMax.x, 1f),
                __instance.Current.rectTransform.anchorMax.y);
        }
    }

    // Highlight compatible slots when dragging mods. Built into the game already, just need to register the inspect window SlotViews
    public class HighlightSlotsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemSpecificationPanel), nameof(ItemSpecificationPanel.Show));
        }

        [PatchPostfix]
        public static void Postfix(ItemSpecificationPanel __instance, ItemContext itemContext, ItemUiContext itemUiContext)
        {
            if (!Settings.HighlightEmptySlots.Value)
            {
                return;
            }

            itemUiContext.RegisterView(itemContext);
            __instance.UI.AddOnDispose(() => itemUiContext.UnregisterView(itemContext));
        }
    }

    public class HighlightFilledSlotsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SlotView), nameof(SlotView.CheckAcceptHandler));
        }

        [PatchPostfix]
        public static void Postfix(SlotView __instance, DragItemContext dragItemContext)
        {
            if (!Settings.HighlightFilledSlots.Value)
            {
                return;
            }

            if (__instance.Slot == null || dragItemContext == null || __instance.Slot.ContainedItem == null)
            {
                return;
            }

            Slot originalSlot = __instance.Slot;
            __instance.Slot = new Slot(originalSlot, originalSlot.ParentItem as CompoundItem);
            __instance.HighlightItemViewPosition(dragItemContext, null, true);
            __instance.Slot = originalSlot;
        }
    }

    public class FixTraderCompatWithPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(ItemUiContext), m => m.Name == "Configure" && m.GetParameters().Any(p => p.Name == "equipment"));
        }

        [PatchPrefix]
        public static void Prefix(ItemController itemController, ref InventoryEquipment equipment)
        {
            if (equipment == null && itemController is InventoryController inventoryController)
            {
                equipment = inventoryController.Inventory.Equipment;
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
        const string increasingColorHex = "#5EC1FF";
        const string decreasingColorHex = "#C40000";

        string text = textMesh.text;
        ItemAttribute attribute = panel.ItemAttribute;

        // Holy shit did they mess up MOA. Half of the calculation is done in the StringValue() method, so calculating delta from Base() loses all that
        // Plus, they round the difference to the nearest integer (!?)
        // Completely redo it
        if (attribute.Id.ToEnum<EItemAttributeId>() == EItemAttributeId.CenterOfImpact)
        {
            var compareAttribute = panel.CompareItemAttribute;
            if (compareAttribute != null)
            {
                string currentStringValue = attribute.StringValue.Invoke();
                var moaMatch = Regex.Match(currentStringValue, @"^(\S+)");
                if (float.TryParse(moaMatch.Groups[1].Value, out float moa))
                {
                    string compareStringValue = compareAttribute.StringValue.Invoke();
                    moaMatch = Regex.Match(compareStringValue, @"^(\S+)");
                    if (float.TryParse(moaMatch.Groups[1].Value, out float compareMoa))
                    {
                        float delta = compareMoa - moa;
                        string final = currentStringValue;
                        if (Math.Abs(delta) > 0)
                        {
                            string sign = delta > 0 ? "+" : "";
                            string color = (attribute.LessIsGood && delta < 0) || (!attribute.LessIsGood && delta > 0) ? increasingColorHex : decreasingColorHex;
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
                string color = (attribute.LessIsGood && value < 0) || (!attribute.LessIsGood && value > 0) ? increasingColorHex : decreasingColorHex;

                // Except some that have a space weren't actually formatted with P1 and are 0-100 with a manually added " %"
                text = NonPercentPercents.Contains(attribute.Id.ToEnum<EItemAttributeId>())
                    ? Regex.Replace(text, @"%\([+-].*\)", "%<color=" + color + ">(" + sign + value + "%)</color>")
                    : Regex.Replace(text, @"%\([+-].*\)", "%<color=" + color + ">(" + sign + value.ToString("P1") + ")</color>");
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
                    string color = (attribute.LessIsGood && value < 0) || (!attribute.LessIsGood && value > 0) ? increasingColorHex : decreasingColorHex;
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
                        string color = (attribute.LessIsGood && value < 0) || (!attribute.LessIsGood && value > 0) ? increasingColorHex : decreasingColorHex;
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

    private static Il2CppCollections.List<ItemAttribute> GetDeepAttributes(Item item, out bool changed)
    {
        changed = false;
        var itemAttributes = item.Attributes.Where(a => a.DisplayType.Invoke() == EItemAttributeDisplayType.Compact).ToIl2CppList();
        foreach (var subItem in item.GetAllItems()) // This get all items, recursively
        {
            if (subItem == item)
            {
                continue;
            }

            var subAttributes = subItem.Attributes.Where(a => a.DisplayType.Invoke() == EItemAttributeDisplayType.Compact).ToIl2CppList();
            itemAttributes = CombineAttributes(itemAttributes, subAttributes);
            changed = true;
        }

        return itemAttributes;
    }

    private static Il2CppCollections.List<ItemAttribute> CombineAttributes(Il2CppCollections.List<ItemAttribute> first, Il2CppCollections.List<ItemAttribute> second)
    {
        Il2CppCollections.List<ItemAttribute> result = [];

        foreach (var id in first.Select(a => a.Id).Union(second.Select(a => a.Id)))
        {
            // Need to cast the id since it's of type Enum for some reason
            var attribute = first.FirstOrDefault(a => a.Id.Equals(id));
            var other = second.FirstOrDefault(a => a.Id.Equals(id));
            if (attribute == null)
            {
                result.Add(other);
            }
            else if (other == null)
            {
                result.Add(attribute);
            }
            else
            {
                var combined = attribute.Clone();
                switch (attribute.Id.ToEnum<EItemAttributeId>())
                {
                    case EItemAttributeId.EffectiveDist:
                    case EItemAttributeId.SightingRange:
                        combined.Base = (Func<float>)(() => Math.Max(attribute.Base.Invoke(), other.Base.Invoke()));
                        combined.StringValue = (Func<string>)(() => combined.Base.Invoke().ToString());
                        break;
                    case EItemAttributeId.Accuracy:
                    case EItemAttributeId.Recoil:
                        combined.Base = (Func<float>)(() => attribute.Base.Invoke() + other.Base.Invoke());
                        combined.StringValue = (Func<string>)(() => combined.Base.Invoke() + "%");
                        break;
                    case EItemAttributeId.Loudness:
                    case EItemAttributeId.Ergonomics:
                    case EItemAttributeId.Velocity:
                        combined.Base = (Func<float>)(() => attribute.Base.Invoke() + other.Base.Invoke());
                        combined.StringValue = (Func<string>)(() => combined.Base.Invoke().ToString());
                        break;
                    case EItemAttributeId.DurabilityBurn:
                    case EItemAttributeId.HeatFactor:
                    case EItemAttributeId.CoolFactor:
                        combined.Base = (Func<float>)(() => attribute.Base.Invoke() + other.Base.Invoke());
                        combined.StringValue = (Func<string>)(() => combined.Base.Invoke().ToString("P1"));
                        break;
                    case EItemAttributeId.RaidModdable:
                        break;
                    default:
                        break;
                }

                result.Add(combined);
            }
        }

        return result;
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