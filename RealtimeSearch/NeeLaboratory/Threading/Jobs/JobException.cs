using System;

namespace NeeLaboratory.Threading.Jobs
{
    /// <summary>
    /// JOB例外
    /// </summary>
    public class JobException : Exception
    {
        /// <summary>
        /// 例外が発生したJOB
        /// </summary>
        private readonly IJob _job;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        /// <param name="job"></param>
        public JobException(string message, Exception inner, IJob job)
            : base(message, inner)
        {
            _job = job;
        }


        /// <summary>
        /// 例外が発生したJOB
        /// </summary>
        public IJob Job => _job;
    }
}
