using Integration.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Service
{
    // This is used to prevent multiple threads from saving the same item content at the same time.
    public class CriticalOperationProcessor<TKey> where TKey : notnull
    {
        // Helper Class to keep track of the number of subscribers to an operation lock.
        private class OperationLock
        {
            int _subscribers = 0;
            readonly object _lock = new();

            public object Subscribe()
            {
                Interlocked.Increment(ref _subscribers);
                return _lock;
            }
            public bool Unsubscribe()
            {
                return Interlocked.Decrement(ref _subscribers) == 0;
            }

        }

        private readonly ConcurrentDictionary<TKey, OperationLock> _currentOperations = new();

        // This method ensures that only one thread at a time can be in the critical section for the same itemContent.
        public Result SafeOperation(TKey itemContent, Func<Result> func)
        {
            // Get the operation lock for the item content if it exists, or create a new one.
            var operationLock = _currentOperations.GetOrAdd(itemContent, new OperationLock());

            // Subscribe to the operation lock for getting the lock object.
            object lockObject = operationLock.Subscribe();

            Monitor.Enter(lockObject);
            try
            {
                return func();
            }
            finally
            {
                Monitor.Exit(lockObject);

                // If unsubscribe returns true, it means that there are no more subscribers to the operation lock.
                if (operationLock.Unsubscribe())
                    _currentOperations.TryRemove(itemContent, out _);
            }
        }
    }
}
