using System;
using EFT.InputSystem;
using UnityEngine;

namespace UIFixes;

public class InputRepeater : MonoBehaviour
{
    private EGameKey _gameKey;
    private Func<bool> _retry;
    private float _timer = 0f;

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
            _gameKey = gameKey;
            _retry = retry;
            enabled = true;
        }
    }

    public void StopTrying()
    {
        Reset();
    }

    private void Reset()
    {
        _gameKey = EGameKey.None;
        _retry = null;
        _timer = 0f;
        enabled = false;
    }

    public void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < 0.1f)
        {
            return;
        }

        _timer -= 0.1f;

        if (InputHelper.IsKeyHeld(_gameKey))
        {
            try
            {
                if (_retry())
                {
                    StopTrying();
                }
            }
            catch (Exception ex)
            {
                Plugin.Instance.Logger.LogError($"Error repeating {_gameKey}: {ex}");
                StopTrying();
            }
        }
        else
        {
            StopTrying();
        }
    }
}