using System.Reflection;
using EFT;
using EFT.InputSystem;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class QueueInputPatches
{
    public static void Enable()
    {
        new AimPatch().Enable();
        new ReloadPatch().Enable();

        new SprintPatch().Enable();
        new PreSprintPatch().Enable();

        new MapKeyBindingsPatch().Enable();
    }

    public class AimPatch : ModulePatch
    {
        private static bool InAttempt = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(Player.FirearmController), nameof(Player.FirearmController.SetAim), [typeof(bool)]);
        }

        [PatchPostfix]
        public static void Postfix(Player.FirearmController __instance, bool value)
        {
            if (!Settings.QueueHeldInputs.Value || InAttempt || __instance == null)
            {
                return;
            }

            InputRepeater repeater;
            if (value && !__instance.IsAiming)
            {
                repeater = __instance.GetOrAddComponent<InputRepeater>();
                repeater.BeginTrying(EGameKey.Aim, () =>
                {
                    if (__instance == null)
                    {
                        repeater.StopTrying();
                        return;
                    }

                    InAttempt = true;
                    __instance.SetAim(true);
                    InAttempt = false;
                });
            }
            else if (!value)
            {
                repeater = __instance.GetComponent<InputRepeater>();
                if (repeater != null)
                {
                    repeater.StopTrying();
                }
            }
        }
    }

    public class ReloadPatch : ModulePatch
    {
        private static bool InAttempt = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(FirearmHandsInputTranslator), nameof(FirearmHandsInputTranslator.method_13));
        }

        [PatchPrefix]
        public static void Postfix(FirearmHandsInputTranslator __instance)
        {
            if (!Settings.QueueHeldInputs.Value || InAttempt || __instance == null)
            {
                return;
            }

            if (__instance.IfirearmHandsController_0 is not Player.FirearmController firearmController)
            {
                return;
            }

            InputRepeater repeater;
            if (!firearmController.CanStartReload())
            {
                repeater = firearmController.GetOrAddComponent<InputRepeater>();
                repeater.BeginTrying(EGameKey.ReloadWeapon, () =>
                {
                    if (__instance == null)
                    {
                        repeater.StopTrying();
                        return;
                    }

                    InAttempt = true;
                    __instance.method_13();
                    InAttempt = false;
                });
            }
            else
            {
                repeater = firearmController.GetComponent<InputRepeater>();
                if (repeater != null)
                {
                    repeater.StopTrying();
                }
            }
        }
    }

    // Replaces Player.ToggleSprint in order to not pass true (isToggle) to EnableSprint()
    // BSG literally wrote the code that allows you to press sprint before you start running, then made that code
    // unreachable because the sprint command is *always* a toggle.
    public class SprintPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.ToggleSprint));
        }

        [PatchPrefix]
        public static bool Prefix(Player __instance)
        {
            if (!Settings.QueueHeldInputs.Value)
            {
                return true;
            }

            bool enable = !__instance.Physical.Sprinting;
            __instance.CurrentManagedState.EnableSprint(enable, false);

            return false;
        }
    }

    // Replaces the first half of this method to avoid the call to EnableSprint(false). This is literally the ramp up to sprint, 
    // but if you aren't going fast enough they just kill the sprint. Why!?
    public class PreSprintPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(MovementContext), nameof(MovementContext.PreSprintAcceleration));
        }

        [PatchPrefix]
        public static bool Prefix(MovementContext __instance, Player ____player)
        {
            if (!Settings.QueueHeldInputs.Value)
            {
                return true;
            }

            if (____player.UsedSimplifiedSkeleton)
            {
                return false;
            }

            if (__instance.MovementDirection.y < 0.1f)
            {
                //__instance.EnableSprint(false); // Why, BSG? It's literally called PreSprintAcceleration, give the guy a chance to get up to speed!
                return false;
            }

            // The rest of the function is fine
            return true;
        }
    }

    public class MapKeyBindingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InputBindingsDataClass), nameof(InputBindingsDataClass.UpdateBindings));
        }

        [PatchPostfix]
        public static void Postfix(InputBindingsDataClass __instance)
        {
            InputRepeater.MapKeyBindings(__instance);
        }
    }
}