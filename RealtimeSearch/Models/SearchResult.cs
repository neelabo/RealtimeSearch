using NeeLaboratory.IO.Search;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NeeLaboratory.RealtimeSearch
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


    public class SearchResult<T> : ISearchResult<T>
        where T : ISearchItem
    {
        public SearchResult(string keyword, IEnumerable<T>? items) : this(keyword, items, null)
        {
        }

        public SearchResult(string keyword, IEnumerable<T>? items, Exception? exception)
        {
            Keyword = keyword;
            Items = new ObservableCollection<T>(items ?? Array.Empty<T>());
            Exception = exception;
        }


        /// <summary>
        /// 検索キーワード
        /// </summary>
        public string Keyword { get; private set; }

        /// <summary>
        /// 検索結果
        /// </summary>
        public ObservableCollection<T> Items { get; private set; }

        /// <summary>
        /// 検索失敗時の例外
        /// </summary>
        public Exception? Exception { get; private set; }
    }

}
