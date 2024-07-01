using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace UIFixes
{
    public class TaskSerializer<T> : MonoBehaviour
    {
        private Func<T, Task> func;
        private IEnumerator<T> enumerator;
        private Task currentTask;
        private TaskCompletionSource totalTask;

        public Task Initialize(IEnumerable<T> items, Func<T, Task> func)
        {
            this.enumerator = items.GetEnumerator();
            this.func = func;

            currentTask = Task.CompletedTask;
            totalTask = new TaskCompletionSource();

            LateUpdate();

            return totalTask.Task;
        }

        public void Cancel()
        {
            totalTask.TrySetCanceled();
            Complete();
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
}
