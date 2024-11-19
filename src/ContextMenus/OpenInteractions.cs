using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UIFixes;

public class OpenInteractions(ItemContextAbstractClass itemContext, ItemUiContext itemUiContext) : ItemInfoInteractionsAbstractClass<OpenInteractions.Options>(itemUiContext)
{
    private readonly ItemContextAbstractClass itemContext = itemContext;

    public override void ExecuteInteractionInternal(Options interaction)
    {
        if (itemContext == null || itemContext.Item is not CompoundItem compoundItem)
        {
            return;
        }

        var taskSerializer = itemUiContext_0.gameObject.AddComponent<NestedContainerTaskSerializer>();
        taskSerializer.Initialize(GetNestedContainers(itemContext), containerContext =>
        {
            if (containerContext != null)
            {
                itemUiContext_0.OpenItem(containerContext.Item as CompoundItem, containerContext);
            }

            return Task.CompletedTask;
        });
    }

    public override bool IsActive(Options button)
    {
        return true;
    }

    public override IResult IsInteractive(Options button)
    {
        return SuccessfulResult.New;
    }

    public override bool HasIcons
    {
        get { return false; }
    }

    public enum Options
    {
        All
    }

    private IEnumerable<ItemContextAbstractClass> GetNestedContainers(ItemContextAbstractClass first)
    {
        var windowRoot = Singleton<PreloaderUI>.Instance;
        CompoundItem parent = first.Item as CompoundItem;

        yield return first;

        while (true)
        {
            var innerContainers = parent.GetFirstLevelItems()
                .Where(i => i != parent)
                .Where(i => i is CompoundItem innerContainer && innerContainer.Grids.Any());
            if (innerContainers.Count() != 1)
            {
                yield break;
            }

            var targetId = innerContainers.First().Id;
            var targetItemView = windowRoot.GetComponentsInChildren<GridItemView>().FirstOrDefault(itemView => itemView.Item.Id == targetId);
            if (targetItemView == null)
            {
                yield return null; // Keeps returning null until the window is open
            }

            parent = targetItemView.Item as CompoundItem;
            yield return targetItemView.ItemContext;
        }
    }
}

public class NestedContainerTaskSerializer : TaskSerializer<ItemContextAbstractClass> { }