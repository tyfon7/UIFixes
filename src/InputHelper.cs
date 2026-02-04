using System.Collections.Generic;
using EFT.InputSystem;

namespace UIFixes;

public static class InputHelper
{
    private static Dictionary<EGameKey, KeyBindingClass> KeyBindings = [];

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
        if (KeyBindings.TryGetValue(gameKey, out KeyBindingClass keyBinding))
        {
            return keyBinding;
        }

        return null;
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
            switch (keyPress)
            {
                case EKeyPress.Hold:
                case EKeyPress.Down:
                    return true;
                case EKeyPress.Up:
                case EKeyPress.None:
                default:
                    return false;
            }
        }

        return false;
    }
}