using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Comfort.Common;
using EFT.Communications;
using EFT.InventoryLogic;
using EFT.UI;

namespace UIFixes;

public class RepairInteractions : ItemInfoInteractionsAbstractClass<RepairInteractions.ERepairers>
{
    private readonly RepairControllerClass repairController;
    private readonly int playerRubles;
    private readonly IEnumerable<R.RepairStrategy> repairStrategies;
    private readonly IEnumerable<IRepairer> repairers;

    public RepairInteractions(Item item, ItemUiContext uiContext, int playerRubles) : this([item], uiContext, playerRubles) { }

    public RepairInteractions(IEnumerable<Item> items, ItemUiContext uiContext, int playerRubles) : base(uiContext)
    {
        repairController = uiContext.Session.RepairController;

        // Add empty action because otherwise RepairControllerClass.action_0 is null and it pukes on successful repair
        repairController.OnSuccessfulRepairChangedEvent += () => { };

        this.playerRubles = playerRubles;

        repairStrategies = items.Select(i => R.RepairStrategy.Create(i, repairController));
        repairers = repairStrategies.SelectMany(rs => rs.Repairers).DistinctBy(r => r.RepairerId);

        Load();
    }

    private void Load()
    {
        foreach (IRepairer repairer in repairers)
        {
            double totalKitAmount = 0f;
            int totalPrice = 0;

            foreach (var repairStrategy in repairStrategies)
            {
                repairStrategy.CurrentRepairer = repairer;

                float repairAmount = GetClampedRepairAmount(repairStrategy);
                if (repairAmount < float.Epsilon || !repairStrategy.CanRepair(repairStrategy.CurrentRepairer, repairStrategy.CurrentRepairer.Targets))
                {
                    continue;
                }
                else if (R.RepairKit.Type.IsInstanceOfType(repairer))
                {
                    var repairKit = new R.RepairKit(repairer);
                    totalKitAmount += repairStrategy.GetRepairPrice(repairAmount, repairKit.Value);
                }
                else
                {
                    totalPrice += repairStrategy.GetCurrencyPrice(repairAmount);
                }
            }

            string text;
            if (totalKitAmount > double.Epsilon)
            {
                var repairKit = new R.RepairKit(repairer);
                float pointsLeft = repairKit.GetRepairPoints();

                string costColor = totalKitAmount > pointsLeft ? "#FF0000" : "#ADB8BC";
                text = string.Format("<b><color=#C6C4B2>{0}</color> <color={1}>({2} {3})</color></b>", repairer.LocalizedName, costColor, Math.Round(totalKitAmount, 2).ToString(CultureInfo.InvariantCulture), "RP".Localized());
            }
            else if (totalPrice > 0)
            {
                string priceColor = totalPrice > playerRubles ? "#FF0000" : "#ADB8BC";
                text = string.Format("<b><color=#C6C4B2>{0}</color> <color={1}>({2} ₽)</color></b>", repairer.LocalizedName, priceColor, totalPrice);
            }
            else
            {
                text = string.Format("<b><color=#C6C4B2>{0}</color></b>", repairer.LocalizedName);
            }

            base.method_2(MakeInteractionId(repairer.RepairerId), text, () => this.Repair(repairer.RepairerId));
        }
    }

    private static float GetClampedRepairAmount(R.RepairStrategy repairStrategy)
    {
        float repairAmount = repairStrategy.HowMuchRepairScoresCanAccept();

        // The repair window round-trips this amount through a UI element that operatoes on percents, so it divides this by the max durability
        // The UI element however has a minimum value of 0.001, which artificially caps how small a repair can be. To emulate this I have to do the same math
        float percentAmount = repairAmount / repairStrategy.TemplateDurability();

        return percentAmount < 0.001f ? 0 : repairAmount;
    }

    private async void Repair(string repairerId)
    {
        bool anySuccess = false;
        foreach (var repairStrategy in repairStrategies)
        {
            var repairer = repairStrategy.Repairers.FirstOrDefault(r => r.RepairerId == repairerId);
            if (repairer == null)
            {
                continue;
            }

            repairStrategy.CurrentRepairer = repairer;
            IResult result = await repairStrategy.RepairItem(repairStrategy.HowMuchRepairScoresCanAccept(), null);
            if (result.Succeed)
            {
                anySuccess = true;
                NotificationManagerClass.DisplayMessageNotification(string.Format("{0} {1:F1}", "Item successfully repaired to".Localized(null), repairStrategy.Durability()), ENotificationDurationType.Default, ENotificationIconType.Default, null);
            }
        }

        if (anySuccess)
        {
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.RepairComplete);
        }
    }

    public IResult GetButtonInteraction(string interactionId)
    {
        string repairerId = interactionId.Split(':')[1];
        IRepairer repairer = repairers.Single(r => r.RepairerId == repairerId);

        if (!repairStrategies.Any(rs => rs.CanRepair(repairer, repairer.Targets)))
        {
            return new FailedResult(ERepairStatusWarning.ExceptionRepairItem.ToString());
        }

        double totalKitAmount = 0f;
        int totalPrice = 0;

        foreach (var repairStrategy in repairStrategies)
        {
            repairStrategy.CurrentRepairer = repairer;

            float repairAmount = GetClampedRepairAmount(repairStrategy);
            if (repairAmount < float.Epsilon || !repairStrategy.CanRepair(repairStrategy.CurrentRepairer, repairStrategy.CurrentRepairer.Targets))
            {
                continue;
            }
            else if (R.RepairKit.Type.IsInstanceOfType(repairer))
            {
                var repairKit = new R.RepairKit(repairer);
                totalKitAmount += repairStrategy.GetRepairPrice(repairAmount, repairKit.Value);
            }
            else
            {
                totalPrice += repairStrategy.GetCurrencyPrice(repairAmount);
            }
        }

        if (totalKitAmount > double.Epsilon && R.RepairKit.Type.IsInstanceOfType(repairer))
        {
            // This check is only for repair kits
            if (repairStrategies.Any(rs => rs.IsNoCorrespondingArea()))
            {
                return new FailedResult(ERepairStatusWarning.NoCorrespondingArea.ToString());
            }

            var repairKit = new R.RepairKit(repairer);
            float pointsLeft = repairKit.GetRepairPoints();
            if (totalKitAmount > pointsLeft)
            {
                return new FailedResult(ERepairStatusWarning.NotEnoughRepairPoints.ToString());
            }
        }
        else if (totalPrice > 0)
        {
            if (totalPrice > playerRubles)
            {
                return new FailedResult(ERepairStatusWarning.NotEnoughMoney.ToString());
            }
        }
        else
        {
            return new FailedResult(ERepairStatusWarning.NothingToRepair.ToString());
        }

        // BrokenItemError is not actually an error, they just implemented it that way - it shows a bunch of red text but it doesn't prevent repair
        // Leaving this here to remember
        // if (repairStrategy.BrokenItemError())
        // {
        //     return new FailedResult(ERepairStatusWarning.BrokenItem.ToString());
        // }

        return SuccessfulResult.New;
    }

    public override void ExecuteInteractionInternal(ERepairers interaction)
    {
    }

    public override bool IsActive(ERepairers button)
    {
        return button == ERepairers.None && !this.repairController.TraderRepairers.Any();
    }

    public override IResult IsInteractive(ERepairers button)
    {
        return new FailedResult("No repairers?", 0);
    }

    public override bool HasIcons
    {
        get { return false; }
    }

    public enum ERepairers
    {
        None
    }
    private static string MakeInteractionId(string traderId)
    {
        return "UIFixesRepairerId:" + traderId;
    }

    public static bool IsRepairInteractionId(string id)
    {
        return id.StartsWith("UIFixesRepairerId:");
    }
}

public static class RepairExtensions
{
    public static bool IsRepairInteraction(this DynamicInteractionClass interaction)
    {
        return interaction.Id.StartsWith("UIFixesRepairerId:");
    }
}
