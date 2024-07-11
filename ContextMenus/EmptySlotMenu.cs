using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Ragfair;
using System;
using System.Collections.Generic;

namespace UIFixes.ContextMenus
{
    public class EmptySlotMenu(Slot slot, ItemContextAbstractClass itemContext, ItemUiContext itemUiContext, Action closeAction) : GClass3042(itemContext, itemUiContext, closeAction)
    {
        private static readonly List<EItemInfoButton> Actions = [EItemInfoButton.LinkedSearch];

        private readonly Slot slot = slot;

        public override IEnumerable<EItemInfoButton> AvailableInteractions => Actions;

        public override void ExecuteInteractionInternal(EItemInfoButton interaction)
        {
            switch (interaction)
            {
                case EItemInfoButton.LinkedSearch:
                    Search(new GClass3219(EFilterType.LinkedSearch, slot.ParentItem.TemplateId + ":" + slot.Id, true));
                    break;
                default:
                    break;
            }
        }
    }
}
