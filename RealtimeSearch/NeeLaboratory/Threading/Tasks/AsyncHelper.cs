using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeeLaboratory.Threading.Tasks
{
    // from https://stackoverflow.com/questions/9343594/how-to-call-asynchronous-method-from-synchronous-method-in-c
    public static class AsyncHelper
    {
        private static readonly TaskFactory _myTaskFactory = new(CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);

        /// <summary>
        /// Asyncメソッドの同期実行
        /// </summary>
        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            return _myTaskFactory
              .StartNew<Task<TResult>>(func)
              .Unwrap<TResult>()
              .GetAwaiter()
              .GetResult();
        }

        /// <summary>
        /// Asyncメソッドの同期実行
        /// </summary>
        public static void RunSync(Func<Task> func)
        {
            _myTaskFactory
              .StartNew<Task>(func)
              .Unwrap()
              .GetAwaiter()
              .GetResult();
        }
    }
}
