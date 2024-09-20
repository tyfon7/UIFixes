﻿using EFT.UI;
using EFT.UI.Chat;
using EFT.UI.Screens;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace UIFixes;

public static class KeepMessagesOpenPatches
{
    private static bool ReopenMessages = false;

    public static void Enable()
    {
        new SniffChatPanelClosePatch().Enable();
        new ReopenMessagesPatch().Enable();
    }

    public class SniffChatPanelClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ChatScreen), nameof(ChatScreen.method_6));
        }

        [PatchPostfix]
        public static void Postfix()
        {
            if (Settings.KeepMessagesOpen.Value)
            {
                ReopenMessages = true;
            }
        }
    }

    public class ReopenMessagesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MainMenuController), nameof(MainMenuController.method_0));
        }

        [PatchPostfix]
        public static void Postfix(MainMenuController __instance, EEftScreenType eftScreenType)
        {
            if (Settings.KeepMessagesOpen.Value && eftScreenType != EEftScreenType.TransferItems && ReopenMessages)
            {
                ReopenMessages = false;
                __instance.ShowScreen(EMenuType.Chat, true);
            }
        }
    }
}
