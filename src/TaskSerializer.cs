using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace UIFixes;

public class TaskSerializer<T> : MonoBehaviour
{
    private Func<T, Task> func;
    private Func<T, bool> canContinue;
    private IEnumerator<T> enumerator;
    private Task currentTask;
    private TaskCompletionSource totalTask;

    public Task Initialize(IEnumerable<T> items, Action<T> action, Func<T, bool> canContinue = null)
    {
        Func<T, Task> func = t =>
        {
            action(t);
            return Task.CompletedTask;
        };

        return Initialize(items, func, canContinue);
    }

    public Task Initialize(IEnumerable<T> items, Func<T, Task> func, Func<T, bool> canContinue = null)
    {
        this.enumerator = items.GetEnumerator();
        this.func = func;
        this.canContinue = canContinue;

        currentTask = Task.CompletedTask;
        totalTask = new TaskCompletionSource();

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
        if (currentTask.IsCanceled)
        {
            Complete();
            return;
        }

        if (totalTask.Task.IsCompleted || !currentTask.IsCompleted)
        {
            return;
        }

        if (canContinue != null && enumerator.Current != null && !canContinue(enumerator.Current))
        {
            return;
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
        func = null;
        Destroy(this);
    }
}
