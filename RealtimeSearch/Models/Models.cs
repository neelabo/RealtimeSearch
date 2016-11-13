using RealtimeSearch.Search;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RealtimeSearch
{
    public class Models : INotifyPropertyChanged
    {
        /// <summary>
        /// PropertyChanged event. 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        /// <summary>
        /// 検索エンジン
        /// </summary>
        public SearchEngine SearchEngine { get; private set; }


        /// <summary>
        /// Files property.
        /// 検索結果
        /// </summary>
        private ObservableCollection<NodeContent> _files;
        public ObservableCollection<NodeContent> Files
        {
            get { return _files; }
            set { if (_files != value) { _files = value; RaisePropertyChanged(); } }
        }


        /// <summary>
        /// Information property.
        /// ステータスバーに表示する情報
        /// </summary>
        private string _information = "";
        public string Information
        {
            get { return _information; }
            set { _information = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// IndexInformation property.
        /// </summary>
        private string _indexInformation = "";
        public string IndexInformation
        {
            get { return _indexInformation; }
            set { _indexInformation = value; RaisePropertyChanged(); }
        }

        Setting _setting;

        //
        public Models(Setting setting)
        {
            //History = new KeywordHistory();

            SearchEngine = new SearchEngine();
            SearchEngine.ResultChanged += (s, e) => SearchEngine_ResultChanged(s);
            SearchEngine.IndexCountChanged += SearchEngine_IndexCountChanged;
            SearchEngine.Start();

            _setting = setting;
            ReIndex();
        }



        // 情報通知
        private void SearchEngine_IndexCountChanged(object sender, string message)
        {
            if (SearchEngine.State == SearchEngineState.Search)
            {
                IndexInformation = message;
            }
            else
            {
                IndexInformation = "";
            }
        }

        // 結果変更
        private void SearchEngine_ResultChanged(object sender)
        {
            Files = SearchEngine.SearchResult;

            if (SearchEngine.SearchResult.Count <= 0)
            {
                Information = "";
            }
            else
            {
                Information = string.Format("{0:#,0} 個の項目", Files.Count);
            }
        }

        /// <summary>
        /// インデックス再構築
        /// </summary>
        public void ReIndex()
        {
            SearchEngine.IndexRequest(_setting.SearchPaths.ToArray());
        }

        /// <summary>
        /// 検索実行
        /// </summary>
        /// <param name="keyword"></param>
        public void Search(string keyword)
        {
            SearchEngine.SearchRequest(keyword, _setting.SearchOption);
        }
    }


}
