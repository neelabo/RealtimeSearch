using NeeLaboratory.Threading.Tasks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NeeLaboratory.Threading.Jobs
{
    public class SlimJob
    {
        protected readonly Delegate _method;
        private readonly CancellationToken _cancellationToken;
        private ManualResetEventSlim? _complete; // = new(false);
        private SlimJobStates _state;
        private Exception? _exception;
        private object? _result;
        private readonly object _lock = new();

        public SlimJob(Delegate callback, CancellationToken cancellationToken)
        {
            _method = callback;
            _cancellationToken = cancellationToken;
            _state = SlimJobStates.Pending;
        }


        public SlimJobStates State => _state;
        public object? Result => _result;
        public Exception? Exception => _exception;


        public void Invoke()
        {
            if (_state != SlimJobStates.Pending) return;
            _state = SlimJobStates.Executing;

            try
            {
                _cancellationToken.ThrowIfCancellationRequested();
                _result = InvokeMethod();
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
            Complete(Exception is null ? SlimJobStates.Completed : SlimJobStates.Aborted);
        }

        protected virtual object? InvokeMethod()
        {
            var callback = (Action)_method;
            callback.Invoke();
            return null;
        }


        public bool Abort()
        {
            if (_state != SlimJobStates.Pending) return false;

            Complete(SlimJobStates.Aborted);
            return true;
        }

        private void Complete(SlimJobStates state)
        {
            lock (_lock)
            {
                if (!_state.IsFinished())
                {
                    _state = state;
                }
                _complete?.Set();
            }
        }

        public Task AsTask()
        {
            lock (_lock)
            {
                if (_state.IsFinished())
                {
                    return Task.CompletedTask;
                }
                _complete ??= new(false);
                return _complete.WaitHandle.AsTask();
            }
        }
    }


    public class SlimJob<TResult> : SlimJob
    {
        public SlimJob(Func<TResult> callback, CancellationToken cancellationToken) : base(callback, cancellationToken)
        {
        }


        public new TResult? Result => base.Result is not null ? (TResult)base.Result : default;


        protected override object? InvokeMethod()
        {
            var callback = (Func<TResult>)_method;
            return callback.Invoke();
        }

        public new async Task<TResult?> AsTask()
        {
            await base.AsTask();
            return Result;
        }
    }
}
