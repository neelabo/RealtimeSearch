using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeeLaboratory.IO.Search
{
    /// <summary>
    /// 検索結果
    /// </summary>
    public class SearchResult
    {
        public string Keyword { get; private set; }
        public SearchOption SearchOption { get; private set; }
        public ObservableCollection<NodeContent> Items { get; private set; }

        public SearchResult(string keyword, SearchOption option, ObservableCollection<NodeContent> items)
        {
            Keyword = keyword;
            SearchOption = option;
            Items = items;
        }
    }

}
