using System.Reflection;

using Comfort.Common;

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

        new BreathPatch().Enable();

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
        public static void Postfix(Player.FirearmController __instance, bool value, Player ____player)
        {
            var firearmController = __instance;
            if (firearmController == null && ____player != null)
            {
                // At certain times during weapon switch, the old firearm controller has been destroyed but is still hooked up, grab the new one
                firearmController = ____player.HandsController as Player.FirearmController;
            }

            if (!Settings.QueueHeldInputs.Value || InAttempt || firearmController == null)
            {
                return;
            }

            InputRepeater repeater;
            if (value && !firearmController.IsAiming)
            {
                repeater = ____player.GetOrAddComponent<InputRepeater>();
                repeater.BeginTrying(EGameKey.Aim, () =>
                {
                    if (firearmController == null)
                    {
                        firearmController = ____player.HandsController as Player.FirearmController;
                        if (firearmController == null)
                        {
                            return true;
                        }
                    }

                    InAttempt = true;
                    firearmController.SetAim(true);
                    InAttempt = false;

                    return firearmController.IsAiming;
                });
            }
            else if (!value)
            {
                repeater = ____player.GetComponent<InputRepeater>();
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
        public static void Postfix(FirearmHandsInputTranslator __instance, Player ___Player_0)
        {
            if (!Settings.QueueHeldInputs.Value || InAttempt)
            {
                return;
            }

            var inputTranslator = __instance;
            var firearmController = inputTranslator.IfirearmHandsController_0 as Player.FirearmController;

            InputRepeater repeater;
            if (firearmController == null || !firearmController.CanStartReload())
            {
                repeater = ___Player_0.GetOrAddComponent<InputRepeater>();
                repeater.BeginTrying(EGameKey.ReloadWeapon, () =>
                {
                    if (firearmController == null)
                    {
                        // No way to tell if inputTranslator is still hooked up, just refresh it for a new firearm controller
                        inputTranslator = GetInputTranslator();
                        if (inputTranslator == null)
                        {
                            return true;
                        }

                        firearmController = inputTranslator.IfirearmHandsController_0 as Player.FirearmController;
                        if (firearmController == null)
                        {
                            // For some reason the firearm controller is destroyed way before the input translator is. Just keep trying
                            return false;
                        }
                    }

                    if (!firearmController.CanStartReload())
                    {
                        return false;
                    }

                    InAttempt = true;
                    inputTranslator.method_13();
                    InAttempt = false;

                    return firearmController.CurrentHandsOperation is FirearmReloadingState;
                });
            }
            else
            {
                repeater = ___Player_0.GetComponent<InputRepeater>();
                if (repeater != null)
                {
                    repeater.StopTrying();
                }
            }
        }

        private static FirearmHandsInputTranslator GetInputTranslator()
        {
            var game = Singleton<AbstractGame>.Instance;
            if (game == null)
            {
                return null;
            }

            if (game is HideoutGame hideoutGame)
            {
                return hideoutGame.PlayerOwner.HandsInputTranslator as FirearmHandsInputTranslator;
            }

            if (game is LocalGame localGame)
            {
                return localGame.PlayerOwner.HandsInputTranslator as FirearmHandsInputTranslator;
            }

            return null;
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

    // Moving while aimed down sights releases hold breath, this patch lets the player enter
    // the hold-breath state when the key is held after moving.
    public class BreathPatch : ModulePatch
    {
        private static readonly AccessTools.FieldRef<MovementContext, Player> PlayerField =
            AccessTools.FieldRefAccess<MovementContext, Player>("_player");

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(IdleStateClass), nameof(IdleStateClass.Enter));
        }

        [PatchPostfix]
        public static void Postfix(IdleStateClass __instance)
        {
            if (!Settings.QueueHeldInputs.Value)
            {
                return;
            }

            var player = PlayerField(__instance.MovementContext);
            if (!player.IsYourPlayer || !player.HandsController.IsAiming)
            {
                return;
            }

            if (!InputHelper.IsKeyHeld(EGameKey.Breath))
            {
                return;
            }

            player.Physical.HoldBreath(true);
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
            InputHelper.MapKeyBindings(__instance);
        }
    }
}