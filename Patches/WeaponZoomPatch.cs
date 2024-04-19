﻿using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.WeaponModding;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine.EventSystems;

namespace UIFixes
{
    public class WeaponZoomPatch
    {
        public static void Enable()
        {
            new EditBuildScreenZoomPatch().Enable();
            new WeaponModdingScreenZoomPatch().Enable();
        }

        public class EditBuildScreenZoomPatch : ModulePatch
        {
            private static ScrollTrigger ScrollTrigger;
            protected override MethodBase GetTargetMethod()
            {
                Type type = typeof(EditBuildScreen);
                return type.GetMethod("Show", [typeof(Item), typeof(Item), typeof(InventoryControllerClass), typeof(ISession)]);
            }

            [PatchPrefix]
            private static void Prefix(EditBuildScreen __instance)
            {
                WeaponPreview weaponPreview = Traverse.Create(__instance).Field("_weaponPreview").GetValue<WeaponPreview>();
                if (ScrollTrigger == null)
                {
                    ScrollTrigger = __instance.gameObject.AddComponent<ScrollTrigger>();
                }

                ScrollTrigger.OnOnScroll += (PointerEventData eventData) =>
                {
                    if (weaponPreview != null && __instance != null)
                    {
                        weaponPreview.Zoom(eventData.scrollDelta.y * 0.12f);
                        __instance.UpdatePositions();
                    }
                };
            }
        }

        public class WeaponModdingScreenZoomPatch : ModulePatch
        {
            private static ScrollTrigger ScrollTrigger;
            protected override MethodBase GetTargetMethod()
            {
                Type type = typeof(WeaponModdingScreen);
                return type.GetMethod("Show", [typeof(Item), typeof(InventoryControllerClass), typeof(LootItemClass[])]);
            }

            [PatchPrefix]
            private static void Prefix(WeaponModdingScreen __instance)
            {
                WeaponPreview weaponPreview = Traverse.Create(__instance).Field("_weaponPreview").GetValue<WeaponPreview>();
                if (ScrollTrigger == null)
                {
                    ScrollTrigger = __instance.gameObject.AddComponent<ScrollTrigger>();
                }

                ScrollTrigger.OnOnScroll += (PointerEventData eventData) =>
                {
                    if (weaponPreview != null && __instance != null)
                    {
                        weaponPreview.Zoom(eventData.scrollDelta.y * 0.12f);
                        __instance.UpdatePositions();
                    }
                };
            }
        }
    }
}
