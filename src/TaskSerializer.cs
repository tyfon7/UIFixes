using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace UIFixes;

public class TaskSerializer<T>
{
    protected static int GlobalDepth = 0;

    private TaskSerializerBehavior _behavior;
    private int _depth;
    private Func<T, Il2CppTasks.Task> _func;
    private Func<T, bool> _canContinue;
    private IEnumerator<T> _enumerator;
    private Il2CppTasks.Task _currentTask;
    private TaskCompletionSource _totalTask;

    public TaskSerializer(GameObject gameObject)
    {
        if (gameObject == null)
        {
            throw new NullReferenceException("gameObject passed to TaskSerializer is null");
        }

        _behavior = gameObject.AddComponent<TaskSerializerBehavior>();
        _behavior.OnLateUpdate = OnLateUpdate;
        _behavior.OnOnDisable = OnOnDisable;
    }

    public Il2CppTasks.Task Initialize(IEnumerable<T> items, Func<T, Il2CppTasks.Task> func, Func<T, bool> canContinue = null)
    {
        if (_behavior == null)
        {
            throw new InvalidOperationException("This TaskSerializer is no longer usable");
        }

        _enumerator = items.GetEnumerator();
        _func = func;
        _canContinue = canContinue;

        _currentTask = null;
        _totalTask = new();

        ++GlobalDepth;
        _depth = GlobalDepth;

        OnLateUpdate();

        return _totalTask.Task;
    }

    public Il2CppTasks.Task Initialize(IEnumerable<T> items, Action<T> action, Func<T, bool> canContinue = null)
    {
        Il2CppTasks.Task Func(T t)
        {
            action(t);
            return Il2CppTasks.Task.CompletedTask;
        }

        return Initialize(items, Func, canContinue != null ? (x) => canContinue(x) : null);
    }

    public void Cancel()
    {
        if (!_totalTask.Task.IsCompleted)
        {
            _totalTask.TrySetCanceled();
            Complete();
        }
    }

    public bool IsCompleted()
    {
        return _totalTask.Task.IsCompleted;
    }

    private void OnLateUpdate()
    {
        if (_totalTask.Task.IsCompleted)
        {
            return;
        }

        // There is a child task serializer running
        if (GlobalDepth > _depth)
        {
            return;
        }

        if (_currentTask != null)
        {
            if (_currentTask.IsCanceled)
            {
                Complete();
                return;
            }

            if (!_currentTask.IsCompleted)
            {
                return;
            }

            if (_canContinue != null && !_canContinue(_enumerator.Current))
            {
                return;
            }
        }

        if (_enumerator.MoveNext())
        {
            _currentTask = _func(_enumerator.Current);
        }
        else
        {
            Complete();
        }
    }

    private void OnOnDisable()
    {
        Complete();
    }

    private void Complete()
    {
        _totalTask.TryComplete();
        --GlobalDepth;
        _func = null;
        UnityEngine.Object.Destroy(_behavior);
        _behavior = null;
    }
}

public class TaskSerializerBehavior : MonoBehaviour
{
    [HideFromIl2Cpp]
    public Action OnLateUpdate { get; set; }

    [HideFromIl2Cpp]
    public Action OnOnDisable { get; set; }

    private void LateUpdate() => OnLateUpdate?.Invoke();

    private void OnDisable() => OnOnDisable?.Invoke();
}