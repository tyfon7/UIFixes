using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Ragfair;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UIFixes;

public static class AddOfferContextMenuPatches
{
    private static Item AddOfferItem = null;

    public static void Enable()
    {
        new AddOfferInventoryMenuPatch().Enable();
        new AddOfferTradingMenuPatch().Enable();
        new AddOfferIsActivePatch().Enable();
        new AddOfferIsInteractivePatch().Enable();
        new AddOfferNameIconPatch().Enable();

        new AddOfferExecutePatch().Enable();
        new ShowAddOfferWindowPatch().Enable();
        new SelectItemPatch().Enable();
    }

    public class AddOfferInventoryMenuPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredProperty(R.InventoryInteractions.CompleteType, "AvailableInteractions").GetMethod;
        }

        [PatchPostfix]
        public static void Postfix(ref IEnumerable<EItemInfoButton> __result)
        {
            if (Settings.AddOfferContextMenu.Value)
            {
                var list = __result.ToList();
                list.Insert(list.IndexOf(EItemInfoButton.Tag), EItemInfoButtonExt.AddOffer);
                __result = list;
            }
        }
    }

    public class AddOfferTradingMenuPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredProperty(R.TradingInteractions.Type, "AvailableInteractions").GetMethod;
        }

        [PatchPostfix]
        public static void Postfix(ref IEnumerable<EItemInfoButton> __result)
        {
            if (Settings.AddOfferContextMenu.Value)
            {
                var list = __result.ToList();
                list.Insert(list.IndexOf(EItemInfoButton.Tag), EItemInfoButtonExt.AddOffer);
                __result = list;
            }
        }
    }

    public class AddOfferNameIconPatch : ModulePatch
    {
        private static Sprite FleaSprite = null;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InteractionButtonsContainer), nameof(InteractionButtonsContainer.Show)).MakeGenericMethod(typeof(EItemInfoButton));
        }

        [PatchPrefix]
        public static void Prefix(ref IReadOnlyDictionary<EItemInfoButton, string> names, ref IReadOnlyDictionary<EItemInfoButton, Sprite> icons)
        {
            names ??= new Dictionary<EItemInfoButton, string>()
            {
                { EItemInfoButtonExt.AddOffer, "ragfair/OFFER ADD" }
            };

            FleaSprite ??= Resources.FindObjectsOfTypeAll<Sprite>().Single(s => s.name == "icon_flea_market");
            icons ??= new Dictionary<EItemInfoButton, Sprite>()
            {
                { EItemInfoButtonExt.AddOffer, FleaSprite }
            };
        }
    }

    public class AddOfferIsActivePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(R.ContextMenuHelper.Type, "IsActive");
        }

        [PatchPrefix]
        public static bool Prefix(EItemInfoButton button, ref bool __result)
        {
            if (button != EItemInfoButtonExt.AddOffer)
            {
                return true;
            }

            if (Plugin.InRaid())
            {
                __result = false;
                return false;
            }

            __result = true;
            return false;
        }
    }

    public class AddOfferIsInteractivePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(R.ContextMenuHelper.Type, "IsInteractive");
        }

        [PatchPostfix]
        public static void Postfix(EItemInfoButton button, ref IResult __result, Item ___item_0)
        {
            if (button != EItemInfoButtonExt.AddOffer)
            {
                return;
            }

            ISession session = PatchConstants.BackEndSession;
            RagFairClass ragfair = session.RagFair;
            if (ragfair.Status != RagFairClass.ERagFairStatus.Available)
            {
                __result = new FailedResult(ragfair.GetFormattedStatusDescription());
                return;
            }

            if (ragfair.MyOffersCount >= ragfair.GetMaxOffersCount(ragfair.MyRating))
            {
                __result = new FailedResult("ragfair/Reached maximum amount of offers");
                return;
            }

            RagfairOfferSellHelperClass ragfairHelper = new(session.Profile, session.Profile.Inventory.Stash.Grid);
            if (!ragfairHelper.method_4(___item_0, out string error))
            {
                __result = new FailedResult(error);
                return;
            }
        }
    }

    public class AddOfferExecutePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BaseItemInfoInteractions), nameof(BaseItemInfoInteractions.ExecuteInteractionInternal));
        }

        [PatchPrefix]
        public static bool Prefix(ItemInfoInteractionsAbstractClass<EItemInfoButton> __instance, EItemInfoButton interaction, Item ___item_0)
        {
            if (interaction != EItemInfoButtonExt.AddOffer)
            {
                return true;
            }

            AddOfferItem = ___item_0;

            __instance.ExecuteInteractionInternal(EItemInfoButton.FilterSearch);
            return false;
        }
    }

    public class ShowAddOfferWindowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RagfairScreen), nameof(RagfairScreen.Show));
        }

        [PatchPostfix]
        public static void Postfix(RagfairScreen __instance)
        {
            if (AddOfferItem == null)
            {
                return;
            }

            __instance.method_27(); // click the add offer button
        }
    }

    public class SelectItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AddOfferWindow), nameof(AddOfferWindow.Show));
        }

        [PatchPostfix]
        public static void Postfix(RagfairOfferSellHelperClass ___ragfairOfferSellHelperClass)
        {
            if (AddOfferItem == null)
            {
                return;
            }

            ___ragfairOfferSellHelperClass.SelectItem(AddOfferItem);
            AddOfferItem = null;
        }
    }
}