﻿using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NeeLaboratory.Threading.Jobs
{
    public class SlimJobOperation
    {
        protected readonly SlimJob _job;

        public SlimJobOperation(SlimJob job)
        {
            _job = job;
        }

        public SlimJob Job => _job;

        public SlimJobStates State => _job.State;

        public Exception? Exception => _job.Exception;

        public object? Result => _job.Result;

        public TaskAwaiter GetAwaiter()
        {
            return _job.AsTask().GetAwaiter();
        }

        public bool Abort()
        {
            return _job.Abort();
        }
    }


    public class SlimJobOperation<TResult> : SlimJobOperation
    {
        public SlimJobOperation(SlimJob<TResult> job) : base(job)
        {
        }

        public new SlimJob<TResult> Job => (SlimJob<TResult>)_job;

        public new TResult? Result => Job.Result;

        public new TaskAwaiter<TResult?> GetAwaiter()
        {
            return Job.AsTask().GetAwaiter();
        }
    }

}