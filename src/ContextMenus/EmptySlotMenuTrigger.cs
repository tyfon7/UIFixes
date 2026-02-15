using System;

using EFT.InventoryLogic;
using EFT.UI;

using UnityEngine;
using UnityEngine.EventSystems;

namespace UIFixes;

public class EmptySlotMenuTrigger : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    private ItemUiContext itemUiContext;
    private Slot slot;
    private ItemContextAbstractClass parentContext;
    private bool hovered = false;

    public void Init(Slot slot, ItemContextAbstractClass parentContext, ItemUiContext itemUiContext)
    {
        this.itemUiContext = itemUiContext;
        this.slot = slot;
        this.parentContext = parentContext;
    }

    public void Update()
    {
        if (!hovered)
        {
            return;
        }

        if (Settings.LinkedSearchKeyBind.Value.IsDown())
        {
            using EmptySlotContext context = new(slot, parentContext, itemUiContext);
            var interactions = itemUiContext.GetItemContextInteractions(context, null);
            interactions.ExecuteInteraction(EItemInfoButton.LinkedSearch);

            // Call this explicitly since screen transition prevents it from firing normally
            OnPointerExit(null);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            EmptySlotContext context = new(slot, parentContext, itemUiContext);
            itemUiContext.ShowContextMenu(context, eventData.position);
        }
    }

    public void OnPointerDown(PointerEventData eventData) { }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
    }

    public void OnPointerUp(PointerEventData eventData) { }
}

public class EmptySlotContext(Slot slot, ItemContextAbstractClass parentContext, ItemUiContext itemUiContext) : ItemContextAbstractClass(parentContext.Item, parentContext.ViewType, parentContext)
{
    private readonly Slot slot = slot;
    private readonly ItemUiContext itemUiContext = itemUiContext;

    public override ItemInfoInteractionsAbstractClass<EItemInfoButton> GetItemContextInteractions(Action closeAction)
    {
        return new EmptySlotMenu(slot, ItemContextAbstractClass, itemUiContext, () =>
        {
            Dispose();
            closeAction?.Invoke();
        });
    }

    public override ItemContextAbstractClass CreateChild(Item item)
    {
        // Should never happen
        throw new NotImplementedException();
    }
}