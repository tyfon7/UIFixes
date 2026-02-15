using System;
using System.Collections.Generic;

using Comfort.Common;

using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Ragfair;

namespace UIFixes;

public class EmptySlotMenu(Slot slot, ItemContextAbstractClass itemContext, ItemUiContext itemUiContext, Action closeAction) : ContextInteractionsAbstractClass(itemContext, itemUiContext, closeAction)
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
        return button switch
        {
            EItemInfoButton.LinkedSearch => !Plugin.InRaid() && AllowedContextTypes.Contains(ItemUiContext_1.ContextType),
            _ => false,
        };
    }

    // Base IsInteractive pukes on mannequin slots, reimpliment it here for linked slots without troublesome parts
    public override IResult IsInteractive(EItemInfoButton button)
    {
        var ragfair = ItemUiContext_1.Session.RagFair;
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
                return new FailedResult("Unsupported button");
        }
    }

    // Base subscriptions to items redraws, but no item means not necessary (and pukes on mannequins)
    public override Action SubscribeOnRedraw() => NoOp;
    public override Action SubscribeOnClose() => NoOp;

    private static void NoOp() { }
}