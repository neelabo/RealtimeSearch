namespace NeeLaboratory.RealtimeSearch
{
    /// <summary>
    /// コマンドエンジン状態
    /// </summary>
    public enum SearchCommandEngineState
    {
        /// <summary>
        /// 処理なし
        /// </summary>
        Idle,

        /// <summary>
        /// 収拾中
        /// </summary>
        Collect,

        /// <summary>
        /// 検索中
        /// </summary>
        Search,
    }
}
