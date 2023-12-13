namespace NeeLaboratory.Threading.Jobs
{
    /// <summary>
    /// Job状態
    /// </summary>
    public enum JobState
    {
        None,

        /// <summary>
        /// 実行中
        /// </summary>
        Run,

        /// <summary>
        /// 完了
        /// </summary>
        Completed,

        /// <summary>
        /// キャンセル
        /// </summary>
        Canceled,

        /// <summary>
        /// 例外終了
        /// </summary>
        Faulted,
    }

}
