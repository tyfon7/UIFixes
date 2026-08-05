using System.Collections.Generic;
using EFT.InputSystem;

namespace UIFixes;

public static class InputHelper
{
    private static readonly Dictionary<EGameKey, InputKeyCombination> KeyBindings = [];

    public static void MapKeyBindings(InputPreset bindingsData)
    {
        KeyBindings.Clear();
        foreach (var entry in bindingsData._workingKeyCombinations)
        {
            if (entry is not InputKeyCombination keyBinding)
            {
                continue;
            }

            KeyBindings[keyBinding.GameKey] = keyBinding;
        }
    }

    public static InputKeyCombination GetKeyBinding(EGameKey gameKey)
    {
        return KeyBindings.TryGetValue(gameKey, out InputKeyCombination keyBinding) ? keyBinding : null;
    }

    public static bool IsKeyHeld(EGameKey gameKey)
    {
        if (!KeyBindings.TryGetValue(gameKey, out InputKeyCombination keyBinding))
        {
            return false;
        }

        if (keyBinding._state.GetKeysStatus(out EKeyPress keyPress))
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