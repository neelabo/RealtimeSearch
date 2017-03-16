using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NeeLaboratory.IO.Search;
using System.Threading;
using System.Diagnostics;

namespace NeeLaboratory.RealtimeSearch
{
    public class MainModel : INotifyPropertyChanged
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
        private SearchEngine SearchEngine { get; set; }


#if false
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
#endif


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

#if false
        /// <summary>
        /// IndexInformation property.
        /// </summary>
        private string _indexInformation = "";
        public string IndexInformation
        {
            get { return _indexInformation; }
            set { _indexInformation = value; RaisePropertyChanged(); }
        }
#endif

        Setting _setting;

        /// <summary>
        /// コンストラクタ
        /// TODO: 設定をわたしているが、設定の読込もここでしょ
        /// </summary>
        /// <param name="setting"></param>
        public MainModel(Setting setting)
        {
            _setting = setting;
            //History = new KeywordHistory();

            SearchEngine = new SearchEngine();
            ////SearchEngine.ResultChanged += (s, e) => SearchEngine_ResultChanged(s);
            ////SearchEngine.IndexCountChanged += SearchEngine_IndexCountChanged;
            SearchEngine.SetSearchAreas(_setting.SearchPaths);
            SearchEngine.Start();

            ////ReIndex();
        }



        // 情報通知
        private void SearchEngine_IndexCountChanged(object sender, string message)
        {
#if false
            if (SearchEngine.State == SearchEngineState.Search)
            {
                IndexInformation = message;
            }
            else
            {
                IndexInformation = "";
            }
#endif
        }

        // 結果変更
        private void SearchEngine_ResultChanged(object sender)
        {
#if false
            Files = SearchEngine.SearchResult;

            if (SearchEngine.SearchResult.Count <= 0)
            {
                Information = "";
            }
            else
            {
                Information = string.Format("{0:#,0} 個の項目", Files.Count);
            }
#endif
        }


        /// <summary>
        /// インデックス再構築
        /// </summary>
        public void ReIndex()
        {
            SearchEngine.SetSearchAreas(_setting.SearchPaths);
            ////SearchEngine.IndexRequest(_setting.SearchPaths.ToArray());
        }


#if false
        /// <summary>
        /// 検索実行
        /// </summary>
        /// <param name="keyword"></param>
        public void Search(string keyword)
        {
            ////SearchEngine.SearchRequest(keyword, _setting.SearchOption);
        }
#endif

        /// <summary>
        /// SearchResult property.
        /// </summary>
        private SearchResult _searchResult;
        public SearchResult SearchResult
        {
            get { return _searchResult; }
            set { if (_searchResult != value) { _searchResult = value; RaisePropertyChanged(); } }
        }


        //
        private CancellationTokenSource _searchCancellationTokenSource;

        /// <summary>
        /// 検索(非同期)
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public async Task SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return;

            try
            {
                // 同時に実行可能なのは1検索のみ。以前の検索はキャンセルして新しい検索コマンドを発行
                _searchCancellationTokenSource?.Cancel();
                _searchCancellationTokenSource = new CancellationTokenSource();

                var searchTask =  SearchEngine.SearchAsync(keyword, _setting.SearchOption, _searchCancellationTokenSource.Token);
                while (true)
                {
                    await Task.Run(() =>searchTask.Wait(1000, _searchCancellationTokenSource.Token));
                    if (searchTask.IsCompleted) break;
                    Information = GetSearchEngineProgress();
                }
                var result = searchTask.Result;
                SearchResult = result;
                Information = $"{result.Items.Count:#,0} 個の項目";
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"Search Canceled: {keyword}");
                //Information = "";
            }
            catch (Exception e)
            {
                Information = e.Message;
                throw;
            }
        }

        private string GetSearchEngineProgress()
        {
            if (SearchEngine.State == SearchEngineState.Collect)
            {
                return $"{SearchEngine.NodeCount:#,0} 個のインデックス作成中...";
            }
            else if (SearchEngine.State == SearchEngineState.Search)
            {
                return $"検索中...";
            }
            else
            {
                return $"処理中...";
            }
        }
    }


}
