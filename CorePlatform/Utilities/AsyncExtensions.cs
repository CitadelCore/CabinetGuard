using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CorePlatform.Utilities
{
    /// <summary>
    /// Partial credit to bj0 @ StackExchange.
    /// </summary>
    public static class AsyncExtensions
    {
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken token)
        {
            TaskCompletionSource<bool> source = new TaskCompletionSource<bool>();
            using (token.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), source))
            {
                if (task != await Task.WhenAny(task, source.Task))
                    throw new OperationCanceledException(token);
            }

            return task.Result;
        }
    }
}
