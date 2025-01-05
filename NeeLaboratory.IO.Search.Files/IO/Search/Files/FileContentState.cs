namespace NeeLaboratory.IO.Search.Files
{
    public enum FileContentState
    {
        // 情報が更新された安定状態
        Stable = 0,

        // 情報の更新が予約済みな状態
        StableReady,

        // キャッシュから生成された情報が不明な状態
        Unknown,

        // 自身の情報は更新されたが、子の情報が不明な状態
        // この状態の場合、子の情報を再生成する必要がある
        UnknownChildren,
    }

    public static class FileContentExtensions
    {
        public static bool IsUnknown(this FileContentState state)
        {
            return state == FileContentState.Unknown || state == FileContentState.UnknownChildren;
        }
    }
}
