using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace UIFixes;

public static class SliderPatches
{
    public static void Enable()
    {
        new IntSliderPatch().Enable();
        new StepSliderPatch().Enable();
    }

    // Change slider values with mouse-wheel
    public class IntSliderPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(IntSlider), nameof(IntSlider.Awake));
        }

        [PatchPostfix]
        public static void Postfix(Slider ____slider)
        {
            ____slider.GetOrAddComponent<SliderMouseListener>().Init(____slider);
        }
    }

    // Change slider values with mouse-wheel
    public class StepSliderPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(StepSlider), nameof(StepSlider.Awake));
        }

        [PatchPostfix]
        public static void Postfix(Slider ____slider)
        {
            ____slider.GetOrAddComponent<SliderMouseListener>().Init(____slider);
        }
    }

    public class SliderMouseListener : MonoBehaviour
    {
        private Slider slider;

        public void Init(Slider slider)
        {
            this.slider = slider;
        }

        public void Update()
        {
            if (slider == null)
            {
                return;
            }

            if (Input.mouseScrollDelta.y > float.Epsilon)
            {
                slider.value = Mathf.Min(slider.value + 1, slider.maxValue);

            }
            else if (Input.mouseScrollDelta.y < -float.Epsilon)
            {
                slider.value = Mathf.Max(slider.value - 1, slider.minValue);
            }
        }
    }
}