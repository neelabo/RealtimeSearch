using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeeLaboratory.Threading.Tasks
{
    public static class WaitHandleExtensions
    {
        /// <summary>
        /// WaitHandle待ちのタスク化。
        /// </summary>
        /// <example>
        /// await ManualResetEventSlim.WaitHandle.AsTask();
        /// </example>
        /// <remarks>
        /// https://docs.microsoft.com/ja-jp/dotnet/standard/asynchronous-programming-patterns/interop-with-other-asynchronous-patterns-and-types
        /// </remarks>
        public static Task AsTask(this WaitHandle waitHandle)
        {
            if (waitHandle == null) throw new ArgumentNullException(nameof(waitHandle));

            var tcs = new TaskCompletionSource<bool>();
            var rwh = ThreadPool.RegisterWaitForSingleObject(waitHandle, delegate { tcs.TrySetResult(true); }, null, -1, true);
            var t = tcs.Task;
            t.ContinueWith((antecedent) => rwh.Unregister(null));
            return t;
        }

        /// <summary>
        /// WaitHandle待ちのタスク化。
        /// AsTask().WaitAsync() を使ったほうが安全かも？
        /// </summary>
        /// <remarks>
        /// https://stackoverflow.com/questions/18756354/wrapping-manualresetevent-as-awaitable-task
        /// </remarks>
        public static Task WaitOneAsync(this WaitHandle waitHandle, CancellationToken cancellationToken, int timeoutMilliseconds = Timeout.Infinite)
        {
            if (waitHandle == null)
                throw new ArgumentNullException(nameof(waitHandle));

            var tcs = new TaskCompletionSource<bool>();
            CancellationTokenRegistration ctr = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            TimeSpan timeout = timeoutMilliseconds > Timeout.Infinite ? TimeSpan.FromMilliseconds(timeoutMilliseconds) : Timeout.InfiniteTimeSpan;

            RegisteredWaitHandle rwh = ThreadPool.RegisterWaitForSingleObject(waitHandle,
                (_, timedOut) =>
                {
                    if (timedOut)
                    {
                        tcs.TrySetCanceled();
                    }
                    else
                    {
                        tcs.TrySetResult(true);
                    }
                },
                null, timeout, true);

            Task<bool> task = tcs.Task;

            _ = task.ContinueWith(s =>
            {
                rwh.Unregister(null);
                return ctr.Unregister();
            }, CancellationToken.None);

            return task;
        }
    }

}
