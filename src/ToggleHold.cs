using EFT.InputSystem;

namespace UIFixes;

public enum ToggleHoldState
{
    Idle = 13,
    ClickOrHold = 14,
    Holding = 15
}

public class ToggleHoldIdleState(KeyBindingClass keyCombination) : KeyBindingClass.KeyCombinationState(keyCombination)
{
    public override ECommand GetCommand(float deltaTime)
    {
        if (!CanProcess())
        {
            return ECommand.None;
        }

        HandleKeys(false);
        KeyCombination.method_0((KeyBindingClass.EKeyState)ToggleHoldState.ClickOrHold);
        return GetCommandInternal();
    }

    protected bool CanProcess()
    {
        return GetKeysStatus(out EKeyPress ekeyPress) && (ekeyPress == EKeyPress.Down);
    }
}

public class ToggleHoldClickOrHoldState(KeyBindingClass keyCombination) : KeyBindingClass.KeyCombinationState(keyCombination)
{
    public override void Enter()
    {
        timer = KeyCombination.DoubleClickTimeout;
    }

    public override ECommand GetCommand(float deltaTime)
    {
        if (GetKeysStatus(out EKeyPress ekeyPress))
        {
            if (ekeyPress == EKeyPress.Hold)
            {
                HandleKeys(false);
                if (LongEnough(deltaTime))
                {
                    KeyCombination.method_0((KeyBindingClass.EKeyState)ToggleHoldState.Holding);
                }

                return ECommand.None;
            }
        }

        UnhandleKeys(null);
        KeyCombination.method_0((KeyBindingClass.EKeyState)ToggleHoldState.Idle);
        return ECommand.None;
    }

    private bool LongEnough(float deltaTime)
    {
        timer -= deltaTime;
        return timer <= 0f;
    }

    private float timer;
}

public class ToggleHoldHoldState(KeyBindingClass keyCombination) : KeyBindingClass.KeyCombinationState(keyCombination)
{
    public override ECommand GetCommand(float deltaTime)
    {
        if (GetKeysStatus(out EKeyPress ekeyPress) && ekeyPress == EKeyPress.Hold)
        {
            HandleKeys(false);
            return ECommand.None;
        }

        UnhandleKeys(null);
        KeyCombination.method_0((KeyBindingClass.EKeyState)ToggleHoldState.Idle);

        if (KeyCombination is ToggleKeyCombination toggleKeyCombination)
        {
            return toggleKeyCombination.Ecommand_1;
        }

        return KeyCombination.Ecommand_0;
    }
}

public static class ToggleHold
{
    public static bool IsEnabled(EGameKey gameKey)
    {
        return gameKey switch
        {
            EGameKey.Aim => Settings.ToggleOrHoldAim.Value,
            EGameKey.Interact => Settings.ToggleOrHoldInteract.Value,
            EGameKey.Tactical => Settings.ToggleOrHoldTactical.Value,
            EGameKey.ToggleGoggles => Settings.ToggleOrHoldGoggles.Value,
            EGameKey.ToggleHeadLight => Settings.ToggleOrHoldHeadlight.Value,
            EGameKey.Sprint => Settings.ToggleOrHoldSprint.Value,
            EGameKey.Slot4 => IsQuickbindEnabled(EGameKey.Slot4),
            EGameKey.Slot5 => IsQuickbindEnabled(EGameKey.Slot5),
            EGameKey.Slot6 => IsQuickbindEnabled(EGameKey.Slot6),
            EGameKey.Slot7 => IsQuickbindEnabled(EGameKey.Slot7),
            EGameKey.Slot8 => IsQuickbindEnabled(EGameKey.Slot8),
            EGameKey.Slot9 => IsQuickbindEnabled(EGameKey.Slot9),
            EGameKey.Slot0 => IsQuickbindEnabled(EGameKey.Slot0),
            _ => false
        };
    }

    private static bool IsQuickbindEnabled(EGameKey gameKey)
    {
        return Quickbind.GetType(gameKey) switch
        {
            Quickbind.ItemType.Tactical => Settings.ToggleOrHoldTactical.Value,
            Quickbind.ItemType.Headlight => Settings.ToggleOrHoldHeadlight.Value,
            Quickbind.ItemType.NightVision => Settings.ToggleOrHoldGoggles.Value,
            _ => false,
        };
    }
}