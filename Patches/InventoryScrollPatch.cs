using Aki.Reflection.Patching;
using EFT.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

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
