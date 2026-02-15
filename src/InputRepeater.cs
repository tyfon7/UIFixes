using System;
using EFT.InputSystem;
using UnityEngine;

namespace UIFixes;

public class InputRepeater : MonoBehaviour
{

    private EGameKey gameKey;
    private Func<bool> retry;

    private float timer = 0f;

    public void BeginTrying(EGameKey gameKey, Func<bool> retry)
    {
        Reset();

        var keyBinding = InputHelper.GetKeyBinding(gameKey);
        if (keyBinding == null)
        {
            return;
        }

        if (keyBinding.Type == EPressType.Press || keyBinding.Type == EPressType.Continuous || ToggleHold.IsEnabled(gameKey))
        {
            this.gameKey = gameKey;
            this.retry = retry;
            enabled = true;
        }
    }

    public void StopTrying()
    {
        Reset();
    }

    private void Reset()
    {
        gameKey = EGameKey.None;
        retry = null;
        enabled = false;
        timer = 0f;
    }

    public void Update()
    {
        timer += Time.deltaTime;
        if (timer < 0.1f)
        {
            return;
        }

        timer -= 0.1f;

        if (InputHelper.IsKeyHeld(gameKey))
        {
            try
            {
                if (retry())
                {
                    StopTrying();
                }
            }
            catch (Exception ex)
            {
                Plugin.Instance.Logger.LogError($"Error repeating {gameKey}: {ex}");
                StopTrying();
            }
        }
        else
        {
            StopTrying();
        }
    }
}