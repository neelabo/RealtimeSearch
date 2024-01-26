using System.Collections.ObjectModel;

namespace NeeLaboratory.IO.Search.Files
{
    public interface ISearchResult<T>
        where T : ISearchItem
    {
        /// <summary>
        /// 検索キーワード
        /// </summary>
        string Keyword { get; }

        /// <summary>
        /// 検索結果
        /// </summary>
        ObservableCollection<T> Items { get; }

        /// <summary>
        /// 検索失敗時の例外
        /// </summary>
        Exception? Exception { get; }
    }

}
