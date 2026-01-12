using System;
using System.Collections.Generic;
using EFT.InputSystem;
using UnityEngine;

namespace UIFixes;

public class InputRepeater : MonoBehaviour
{
    private static Dictionary<EGameKey, KeyBindingClass> KeyBindings = [];

    private KeyBindingClass keyBinding;
    private Action action;

    public static void MapKeyBindings(InputBindingsDataClass bindingsData)
    {
        KeyBindings.Clear();
        foreach (var entry in bindingsData.Gclass2408_0)
        {
            if (entry is not KeyBindingClass keyBinding)
            {
                continue;
            }

            KeyBindings[keyBinding.GameKey] = keyBinding;
        }
    }

    public void BeginTrying(EGameKey gameKey, Action action)
    {
        Reset();

        if (!KeyBindings.TryGetValue(gameKey, out KeyBindingClass keyBinding))
        {
            return;
        }

        if (keyBinding.Type == EPressType.Press || keyBinding.Type == EPressType.Continuous || ToggleHold.IsEnabled(gameKey))
        {
            this.keyBinding = keyBinding;
            this.action = action;
            enabled = true;
        }
    }

    public void StopTrying()
    {
        Reset();
    }

    private void Reset()
    {
        keyBinding = null;
        action = null;
        enabled = false;
    }

    public void Update()
    {
        // KeyCombinationState_0 is the current state
        if (keyBinding.KeyCombinationState_0.GetKeysStatus(out EKeyPress keyPress))
        {
            switch (keyPress)
            {
                case EKeyPress.Hold:
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        Plugin.Instance.Logger.LogError($"Error repeating {keyBinding.GameKey}: {ex}");
                        StopTrying();
                    }
                    break;
                case EKeyPress.None:
                case EKeyPress.Up:
                    StopTrying();
                    break;
                case EKeyPress.Down:
                    // Initial frame?
                    break;
            }
        }
    }
}