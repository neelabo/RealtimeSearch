using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeeLaboratory.Threading
{
    public static class SemaphoreSlimExtensions
    {
        public static IDisposable Lock(this SemaphoreSlim semaphore)
        {
            semaphore.Wait();
            return new Handler(semaphore);
        }

        public static IDisposable Lock(this SemaphoreSlim semaphore, CancellationToken token)
        {
            semaphore.Wait(token);
            return new Handler(semaphore);
        }

        public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphore, CancellationToken token)
        {
            await semaphore.WaitAsync(token);
            return new Handler(semaphore);
        }

        private sealed class Handler : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;
            private bool _disposed = false;

            public Handler(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _semaphore.Release();
                    _disposed = true;
                }
            }
        }
    }
}
