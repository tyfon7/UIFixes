using EFT.InputSystem;

namespace UIFixes
{
    public enum ToggleHoldState
    {
        Idle = 13,
        ClickOrHold = 14,
        Holding = 15
    }

    public class ToggleHoldIdleState(GClass1911 keyCombination) : GClass1911.KeyCombinationState(keyCombination)
    {
        public override ECommand GetCommand(float deltaTime)
        {
            if (!CanProcess())
            {
                return ECommand.None;
            }

            HandleKeys(false);
            KeyCombination.method_0((GClass1911.EKeyState)ToggleHoldState.ClickOrHold);
            return GetCommandInternal();
        }

        protected bool CanProcess()
        {
            return GetKeysStatus(out EKeyPress ekeyPress) && (ekeyPress == EKeyPress.Down);
        }
    }

    public class ToggleHoldClickOrHoldState(GClass1911 keyCombination) : GClass1911.KeyCombinationState(keyCombination)
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
                        KeyCombination.method_0((GClass1911.EKeyState)ToggleHoldState.Holding);
                    }

                    return ECommand.None;
                }
            }

            UnhandleKeys(null);
            KeyCombination.method_0((GClass1911.EKeyState)ToggleHoldState.Idle);
            return ECommand.None;
        }

        private bool LongEnough(float deltaTime)
        {
            timer -= deltaTime;
            return timer <= 0f;
        }

        private float timer;
    }

    public class ToggleHoldHoldState(GClass1911 keyCombination, ECommand disableCommand) : GClass1911.KeyCombinationState(keyCombination)
    {
        private readonly ECommand disableCommand = disableCommand;

        public override ECommand GetCommand(float deltaTime)
        {
            if (GetKeysStatus(out EKeyPress ekeyPress) && ekeyPress == EKeyPress.Hold)
            {
                HandleKeys(false);
                return ECommand.None;
            }

            UnhandleKeys(null);
            KeyCombination.method_0((GClass1911.EKeyState)ToggleHoldState.Idle);
            return disableCommand;
        }
    }
}
