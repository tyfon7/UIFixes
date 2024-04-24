using System.Reflection;

namespace UIFixes
{
    public class InventoryScrollPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SimpleStashPanel).GetMethod(nameof(SimpleStashPanel.Show));
        }

        [PatchPrefix]
        public static void Prefix(ScrollRect ____stashScroll)
        {
            if (Settings.FasterInventoryScroll.Value)
            {
                ____stashScroll.scrollSensitivity = Settings.FasterInventoryScrollSpeed.Value;
            }
            else
            {
                ____stashScroll.scrollSensitivity = 63;
            }
        }
    }
}
