using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Ragfair;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UIFixes;

public class EmptySlotMenu(Slot slot, ItemContextAbstractClass itemContext, ItemUiContext itemUiContext, Action closeAction) : BaseItemInfoInteractions(itemContext, itemUiContext, closeAction)
{
    private static readonly List<EItemInfoButton> Actions = [EItemInfoButton.LinkedSearch];

    private static readonly List<EItemUiContextType> AllowedContextTypes = [
        EItemUiContextType.TraderScreen,
        EItemUiContextType.InventoryScreen,
        EItemUiContextType.RagfairScreen,
        EItemUiContextType.Hideout,
        EItemUiContextType.TransferItemsScreen];

    private readonly Slot slot = slot;

    public override IEnumerable<EItemInfoButton> AvailableInteractions => Actions;

    public override void ExecuteInteractionInternal(EItemInfoButton interaction)
    {
        switch (interaction)
        {
            case EItemInfoButton.LinkedSearch:
                Search(new(EFilterType.LinkedSearch, slot.ParentItem.TemplateId + ":" + slot.ID, true));
                break;
            default:
                break;
        }
    }

    // Base IsActive pukes on mannequin slots, reimpliment it here for linked slots without troublesome parts
    public override bool IsActive(EItemInfoButton button)
    {
        switch (button)
        {
            case EItemInfoButton.LinkedSearch:
                return !Plugin.InRaid() && AllowedContextTypes.Contains(itemUiContext_1.ContextType);
            default:
                return base.IsActive(button);
        }
    }

    // Base IsInteractive pukes on mannequin slots, reimpliment it here for linked slots without troublesome parts
    public override IResult IsInteractive(EItemInfoButton button)
    {
        var ragfair = itemUiContext_1.Session.RagFair;
        switch (button)
        {
            case EItemInfoButton.LinkedSearch:
                if (ragfair == null)
                {
                    return new FailedResult("You can't use flea market right now", 0);
                }

                if (ragfair.Disabled)
                {
                    return new FailedResult(ragfair.GetFormattedStatusDescription(), 0);
                }

                return SuccessfulResult.New;
            default:
                return base.IsInteractive(button);
        }
    }

    // Base subscriptions to items redraws, but no item means not necessary (and pukes on mannequins)
    public override Action SubscribeOnRedraw() => NoOp;
    public override Action SubscribeOnClose() => NoOp;

    private static void NoOp() { }
}