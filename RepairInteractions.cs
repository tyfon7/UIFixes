using Comfort.Common;
using EFT.Communications;
using EFT.InventoryLogic;
using EFT.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace UIFixes
{
    public class RepairInteractions : ItemInfoInteractionsAbstractClass<RepairInteractions.ERepairers>
    {
        private readonly RepairControllerClass repairController;
        private readonly int playerRubles;
        private readonly GInterface33 repairStrategy;

        public RepairInteractions(Item item, ItemUiContext uiContext, int playerRubles) : base(uiContext)
        {
            repairController = uiContext.Session.RepairController;

            // Add empty action because otherwise RepairControllerClass.action_0 is null and it pukes on successful repair
            repairController.OnSuccessfulRepairChangedEvent += () => {};

            this.playerRubles = playerRubles;

            repairStrategy = item.GetItemComponent<ArmorHolderComponent>() != null ?
                new GClass804(item, repairController) :
                new GClass803(item, repairController);

            Load();
        }

        private void Load()
        {
            foreach (IRepairer repairer in repairStrategy.Repairers)
            {
                repairStrategy.CurrentRepairer = repairer;
                float repairAmount = repairStrategy.HowMuchRepairScoresCanAccept();

                string text;
                if (repairAmount < float.Epsilon)
                {
                    text = string.Format("<b><color=#C6C4B2>{0}</color></b>", repairer.LocalizedName);
                }
                else if (repairer is GClass802 repairKit)
                {
                    float pointsLeft = repairKit.GetRepairPoints();
                    double amount = repairStrategy.GetRepairPrice(repairAmount, repairKit);

                    string costColor = amount > pointsLeft ? "#FF0000" : "#ADB8BC";
                    text = string.Format("<b><color=#C6C4B2>{0}</color> <color={1}>({2} {3})</color></b>", repairer.LocalizedName, costColor, Math.Round(amount, 2).ToString(CultureInfo.InvariantCulture), "RP".Localized());
                }
                else
                {
                    int price = repairStrategy.GetCurrencyPrice(repairAmount);

                    string priceColor = price > playerRubles ? "#FF0000" : "#ADB8BC";
                    text = string.Format("<b><color=#C6C4B2>{0}</color> <color={1}>({2} ₽)</color></b>", repairer.LocalizedName, priceColor, price);
                }
                
                base.method_2(MakeInteractionId(repairer.RepairerId), text, () => this.Repair(repairer.RepairerId));
            }
        }

        private async void Repair(string repairerId)
        {
            repairStrategy.CurrentRepairer = repairStrategy.Repairers.Single(r => r.RepairerId == repairerId);
            IResult result = await repairStrategy.RepairItem(repairStrategy.HowMuchRepairScoresCanAccept(), null);
            if (result.Succeed)
            {
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.RepairComplete);
                NotificationManagerClass.DisplayMessageNotification(string.Format("{0} {1:F1}", "Item successfully repaired to".Localized(null), repairStrategy.Durability()), ENotificationDurationType.Default, ENotificationIconType.Default, null);
            }
        }

        public IResult GetButtonInteraction(string interactionId)
        {
            string repairerId = interactionId.Split(':')[1];
            IRepairer repairer = repairStrategy.Repairers.Single(r => r.RepairerId == repairerId);
            repairStrategy.CurrentRepairer = repairer;

            float repairAmount = repairStrategy.HowMuchRepairScoresCanAccept();

            if (repairer is GClass802 repairKit)
            {
                float pointsLeft = repairKit.GetRepairPoints();
                double amount = repairStrategy.GetRepairPrice(repairAmount, repairKit);
                if (amount > pointsLeft)
                {
                    return new FailedResult(ERepairStatusWarning.NotEnoughRepairPoints.ToString());
                }
            }
            else
            {
                int price = repairStrategy.GetCurrencyPrice(repairAmount);
                if (price > playerRubles)
                {
                    return new FailedResult(ERepairStatusWarning.NotEnoughMoney.ToString());
                }
            }

            if (repairAmount < float.Epsilon)
            {
                return new FailedResult(ERepairStatusWarning.NothingToRepair.ToString());
            }

            if (repairStrategy.BrokenItemError())
            {
                return new FailedResult(ERepairStatusWarning.BrokenItem.ToString());
            }

            if (repairStrategy.IsNoCorrespondingArea())
            {
                return new FailedResult(ERepairStatusWarning.NoCorrespondingArea.ToString());
            }

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
}
