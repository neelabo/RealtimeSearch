using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace NeeLaboratory.Threading.Jobs
{
    public class SlimJobThreadEngine : IDisposable
    {
        private readonly object _lock = new();
        private readonly Thread _thread;
        private readonly Queue<SlimJob> _queue = new();
        private bool _disposedValue;
        private readonly ManualResetEventSlim _readyEvent = new(false);
        private readonly CancellationTokenSource _cancellationTokenSource = new();


        public SlimJobThreadEngine(string name)
        {
            _thread = new Thread(Worker);
            _thread.IsBackground = true;
            _thread.Name = name;
            _thread.Start();
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                    _readyEvent.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Invoke(Action action)
        {
            Invoke(action, CancellationToken.None);
        }

        public void Invoke(Action action, CancellationToken cancellationToken)
        {
            var operation =  InvokeAsync(action, cancellationToken);
            operation.Wait(cancellationToken);
        }

        public TResult? Invoke<TResult>(Func<TResult> action)
        {
            return Invoke(action, CancellationToken.None);
        }

        public TResult? Invoke<TResult>(Func<TResult> action, CancellationToken cancellationToken)
        {
            var operation = InvokeAsync(action, cancellationToken);
            operation.Wait(cancellationToken);
            return operation.Result;
        }

        public SlimJobOperation InvokeAsync(Action action)
        {
            return InvokeAsync(action, CancellationToken.None);
        }

        public SlimJobOperation InvokeAsync(Action action, CancellationToken cancellationToken)
        {
            var job = new SlimJob(action, cancellationToken);
            Enqueue(job);
            return new SlimJobOperation(job);
        }

        public SlimJobOperation<TResult> InvokeAsync<TResult>(Func<TResult> action)
        {
            return InvokeAsync(action, CancellationToken.None);
        }

        public SlimJobOperation<TResult> InvokeAsync<TResult>(Func<TResult> action, CancellationToken cancellationToken)
        {
            var job = new SlimJob<TResult>(action, cancellationToken);
            Enqueue(job);
            return new SlimJobOperation<TResult>(job);
        }


        private void Enqueue(SlimJob job)
        {
            lock (_lock)
            {
                if (_disposedValue) return;
                _queue.Enqueue(job);
                _readyEvent.Set();
            }
        }

        private void Worker()
        {
            try
            {
                while (true)
                {
                    if (_disposedValue) break;
                    _readyEvent.Wait(_cancellationTokenSource.Token);

                    SlimJob job;

                    lock (_lock)
                    {
                        if (_queue.Count <= 0)
                        {
                            _readyEvent.Reset();
                            continue;
                        }
                        job = _queue.Dequeue();
                    }

                    job.Invoke();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception)
            {
                throw;
            }

            lock (_lock)
            {
                foreach(var job in _queue)
                {
                    job.Abort();
                }
                _queue.Clear();
            }

            Debug.WriteLine($"Thread.Completed: {Thread.CurrentThread.ManagedThreadId}");
        }
    }
}
