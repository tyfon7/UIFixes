using Aki.Reflection.Patching;
using EFT.Hideout;
using HarmonyLib;
using System.Reflection;

namespace UIFixes
{
    public static class HideoutLevelPatches
    {
        private static string CurrentArea;
        private static ELevelType CurrentLevel = ELevelType.NotSet;

        public static void Enable()
        {
            new SelectAreaPatch().Enable();
            new ChangeLevelPatch().Enable();
            new PickInitialLevelPatch().Enable();
            new ClearLevelPatch().Enable();
        }

        public class SelectAreaPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(AreaScreenSubstrate), nameof(AreaScreenSubstrate.SelectArea));
            }

            [PatchPrefix]
            public static void Prefix(AreaData areaData)
            {
                if (areaData.Template.Id != CurrentArea)
                {
                    CurrentArea = areaData.Template.Id;
                    CurrentLevel = ELevelType.NotSet;
                }
            }
        }

        public class ChangeLevelPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(AreaScreenSubstrate), nameof(AreaScreenSubstrate.method_6));
            }

            [PatchPrefix]
            public static void Prefix(ELevelType state)
            {
                CurrentLevel = state;
            }
        }

        public class PickInitialLevelPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(AreaScreenSubstrate), nameof(AreaScreenSubstrate.method_3));
            }

            [PatchPrefix]
            public static bool Prefix(ref ELevelType __result)
            {
                if (CurrentLevel != ELevelType.NotSet) {
                    __result = CurrentLevel;
                    return false;
                }

                return true;
            }
        }

        public class ClearLevelPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(HideoutScreenOverlay), nameof(HideoutScreenOverlay.ReturnToPreviousState));
            }

            [PatchPostfix]
            public static void Postfix()
            {
                CurrentArea = null;
                CurrentLevel = ELevelType.NotSet;
            }
        }
    }
}
