using System;
using System.IO;

namespace NeeLaboratory.Threading.Jobs
{
    /// <summary>
    /// JOBエラーイベントパラメータ
    /// </summary>
    public class JobErrorEventArgs : ErrorEventArgs
    {
        /// <summary>
        /// 例外が発生したJOB
        /// </summary>
        private readonly IJob? _job;

        /// <summary>
        /// 例外処理済みフラグ
        /// </summary>
        private bool _handled;



        /// <summary>
        /// construcotr
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="job"></param>
        public JobErrorEventArgs(Exception exception, IJob? job) : base(exception)
        {
            _job = job;
        }



        /// <summary>
        /// 例外が発生したJOB
        /// </summary>
        public IJob? Job => _job;

        /// <summary>
        /// 例外処理済みフラグ.
        /// trueにした場合、例外処理済みとして継続処理される。falseの場合、JobEngineタスクの外部に例外を投げる
        /// trueにするとfalseに戻せない。
        /// </summary>
        public bool Handled
        {
            get { return _handled; }
            set
            {
                // Only allow to be set true.
                if (value == true)
                {
                    _handled = value;
                }
            }
        }

    }
}
