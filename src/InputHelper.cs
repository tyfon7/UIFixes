using System.Collections.Generic;
using EFT.InputSystem;

namespace UIFixes;

public static class InputHelper
{
    private static readonly Dictionary<EGameKey, KeyBindingClass> KeyBindings = [];

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

    public static KeyBindingClass GetKeyBinding(EGameKey gameKey)
    {
        return KeyBindings.TryGetValue(gameKey, out KeyBindingClass keyBinding) ? keyBinding : null;
    }

    public static bool IsKeyHeld(EGameKey gameKey)
    {
        if (!KeyBindings.TryGetValue(gameKey, out KeyBindingClass keyBinding))
        {
            return false;
        }

        // KeyCombinationState_0 is the current state
        if (keyBinding.KeyCombinationState_0.GetKeysStatus(out EKeyPress keyPress))
        {
            return keyPress switch
            {
                EKeyPress.Hold or EKeyPress.Down => true,
                _ => false,
            };
        }

        return false;
    }
}