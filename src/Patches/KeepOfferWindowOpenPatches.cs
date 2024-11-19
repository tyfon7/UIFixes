// using EFT.UI.Ragfair;
// using HarmonyLib;
// using SPT.Reflection.Patching;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using System.Threading.Tasks;

// namespace UIFixes;

// public static class KeepOfferWindowOpenPatches
// {
//     private static bool BlockClose = false;

//     private static TaskCompletionSource CompletionSource;
//     private static readonly List<Task> AddOfferTasks = [];
//     private static AddOfferWindow Window;

//     public static void Enable()
//     {
//         new GetTaskCompletionSourcePatch().Enable();
//         new PlaceOfferClickPatch().Enable();
//         new ClosePatch().Enable();
//         new ManageTaskPatch().Enable();
//     }

//     public class GetTaskCompletionSourcePatch : ModulePatch
//     {
//         protected override MethodBase GetTargetMethod()
//         {
//             return AccessTools.DeclaredMethod(typeof(AddOfferWindow), nameof(AddOfferWindow.Show));
//         }

//         [PatchPostfix]
//         public static void Postfix(AddOfferWindow __instance, ref Task __result)
//         {
//             if (!Settings.KeepAddOfferOpen.Value)
//             {
//                 return;
//             }

//             Window = __instance;
//             AddOfferTasks.Clear();

//             // Use a different task to mark when everything is actually done
//             CompletionSource = new TaskCompletionSource();
//             __result = CompletionSource.Task;
//         }
//     }

//     public class PlaceOfferClickPatch : ModulePatch
//     {
//         protected override MethodBase GetTargetMethod()
//         {
//             return AccessTools.Method(typeof(AddOfferWindow), nameof(AddOfferWindow.method_1));
//         }

//         [PatchPrefix]
//         public static void Prefix(AddOfferWindow __instance, TaskCompletionSource ___taskCompletionSource_0, ref TaskCompletionSource __state)
//         {
//             if (!Settings.KeepAddOfferOpen.Value)
//             {
//                 return;
//             }

//             __state = ___taskCompletionSource_0;

//             // Close the window if you're gonna hit max offers
//             var ragfair = __instance.R().Ragfair;
//             if (Settings.KeepAddOfferOpenIgnoreMaxOffers.Value || ragfair.MyOffersCount + 1 < ragfair.GetMaxOffersCount(ragfair.MyRating))
//             {
//                 BlockClose = true;
//             }
//         }

//         [PatchPostfix]
//         public static void Postfix(RequirementView[] ____requirementViews, TaskCompletionSource ___taskCompletionSource_0, ref TaskCompletionSource __state)
//         {
//             BlockClose = false;

//             if (!Settings.KeepAddOfferOpen.Value)
//             {
//                 return;
//             }

//             // If the taskCompletionSource member was changed, then it's adding an offer :S
//             if (__state != ___taskCompletionSource_0)
//             {
//                 AddOfferTasks.Add(__state.Task); // This is the task completion source passed into the add offer call

//                 // clear old prices
//                 foreach (var requirementView in ____requirementViews)
//                 {
//                     requirementView.ResetRequirementInformation();
//                 }
//             }
//         }
//     }

//     public class ClosePatch : ModulePatch
//     {
//         protected override MethodBase GetTargetMethod()
//         {
//             return AccessTools.Method(typeof(AddOfferWindow), nameof(Window.Close));
//         }

//         [PatchPrefix]
//         public static bool Prefix()
//         {
//             if (!Settings.KeepAddOfferOpen.Value)
//             {
//                 return true;
//             }

//             if (!BlockClose && CompletionSource != null && AddOfferTasks.All(t => t.IsCompleted))
//             {
//                 CompletionSource.Complete();
//                 CompletionSource = null;
//                 AddOfferTasks.Clear();
//                 Window = null;
//             }

//             return !BlockClose;
//         }
//     }

//     // The window has a task completion source that completes when closing window or upon successful offer placement (which assumes window closes too)
//     // Replace implementation to ensure it only completes when window is closed, or placement is successful AND window has since closed
//     public class ManageTaskPatch : ModulePatch
//     {
//         protected override MethodBase GetTargetMethod()
//         {
//             Type type = typeof(AddOfferWindow).GetNestedTypes().Single(t => t.GetField("completionSource") != null); // AddOfferWindow.Class3068
//             return AccessTools.Method(type, "method_0");
//         }

//         [PatchPrefix]
//         public static bool Prefix(TaskCompletionSource ___completionSource)
//         {
//             if (!Settings.KeepAddOfferOpen.Value || Window == null)
//             {
//                 return true;
//             }

//             ___completionSource.Complete();
//             if (!Window.gameObject.activeInHierarchy && AddOfferTasks.All(t => t.IsCompleted))
//             {
//                 CompletionSource.Complete();
//                 CompletionSource = null;
//                 AddOfferTasks.Clear();
//                 Window = null;
//             }

//             return false;
//         }
//     }
// }
