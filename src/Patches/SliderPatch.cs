using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
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
        private Slider _slider;

        public void Init(Slider slider)
        {
            _slider = slider;
        }

        public void Update()
        {
            if (_slider == null)
            {
                return;
            }

            if (Input.mouseScrollDelta.y > float.Epsilon)
            {
                _slider.value = Mathf.Min(_slider.value + 1, _slider.maxValue);

            }
            else if (Input.mouseScrollDelta.y < -float.Epsilon)
            {
                _slider.value = Mathf.Max(_slider.value - 1, _slider.minValue);
            }
        }
    }
}