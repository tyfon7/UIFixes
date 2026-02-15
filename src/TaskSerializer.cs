using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

namespace UIFixes;

public class TaskSerializerBase : MonoBehaviour
{
    protected static int GlobalDepth = 0;
}

public class TaskSerializer<T, TaskT> : TaskSerializerBase
{
    private int depth;
    private Func<T, Task<TaskT>> func;
    private Func<T, TaskT, bool> canContinue;
    private IEnumerator<T> enumerator;
    private Task<TaskT> currentTask;
    private TaskCompletionSource totalTask;

    public Task Initialize(IEnumerable<T> items, Func<T, Task<TaskT>> func, Func<T, TaskT, bool> canContinue = null)
    {
        this.enumerator = items.GetEnumerator();
        this.func = func;
        this.canContinue = canContinue;

        currentTask = null;
        totalTask = new TaskCompletionSource();

        ++GlobalDepth;
        depth = GlobalDepth;

        LateUpdate();

        return totalTask.Task;
    }

    public void Cancel()
    {
        if (!totalTask.Task.IsCompleted)
        {
            totalTask.TrySetCanceled();
            Complete();
        }
    }

    public void OnDisable()
    {
        Cancel();
    }

    public void LateUpdate()
    {
        if (totalTask.Task.IsCompleted)
        {
            return;
        }

        // There is a child task serializer running
        if (GlobalDepth > depth)
        {
            return;
        }

        if (currentTask != null)
        {
            if (currentTask.IsCanceled)
            {
                Complete();
                return;
            }

            if (!currentTask.IsCompleted)
            {
                return;
            }

            if (canContinue != null && !canContinue(enumerator.Current, currentTask.Result))
            {
                return;
            }
        }

        if (enumerator.MoveNext())
        {
            currentTask = func(enumerator.Current);
        }
        else
        {
            Complete();
        }
    }

    private void Complete()
    {
        totalTask.TryComplete();
        --GlobalDepth;
        func = null;
        Destroy(this);
    }
}

public class TaskSerializer<T> : TaskSerializer<T, bool>
{
    public Task Initialize(IEnumerable<T> items, Action<T> action, Func<T, bool> canContinue = null)
    {
        Func<T, Task<bool>> func = t =>
        {
            action(t);
            return Task.FromResult(true);
        };

        return base.Initialize(items, func, canContinue != null ? (x, _) => canContinue(x) : null);
    }

    public Task Initialize(IEnumerable<T> items, Func<T, Task> func, Func<T, bool> canContinue = null)
    {
        Func<T, Task<bool>> wrapper = async t =>
        {
            try
            {
                await func(t);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        };

        return base.Initialize(items, wrapper, canContinue != null ? (x, _) => canContinue(x) : null);
    }

}