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
        private SearchEngine _searchEngine;


        /// <summary>
        /// IsBusy property.
        /// </summary>
        private bool _IsBusy;
        public bool IsBusy
        {
            get { return _IsBusy; }
            set { if (_IsBusy != value) { _IsBusy = value; RaisePropertyChanged(); } }
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


        Setting _setting;

        /// <summary>
        /// コンストラクタ
        /// TODO: 設定をわたしているが、設定の読込もここでしょ
        /// </summary>
        /// <param name="setting"></param>
        public Models(Setting setting)
        {
            _setting = setting;

            _searchEngine = new SearchEngine();
            _searchEngine.SetSearchAreas(_setting.SearchPaths);
            _searchEngine.Start();
        }


        /// <summary>
        /// インデックス再構築
        /// </summary>
        public void ReIndex()
        {
            _searchEngine.SetSearchAreas(_setting.SearchPaths);
        }


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

                var searchTask = _searchEngine.SearchAsync(keyword, _setting.SearchOption, _searchCancellationTokenSource.Token);
                while (true)
                {
                    await Task.Run(async () =>
                    {
                        await Task.Yield();
                        searchTask.Wait(1000, _searchCancellationTokenSource.Token);
                    });
                    if (searchTask.IsCompleted) break;
                    IsBusy = true;
                    Information = GetSearchEngineProgress();
                }
                var result = searchTask.Result;
                SearchResult = result;

                IsBusy = false;
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string GetSearchEngineProgress()
        {
            if (_searchEngine.State == SearchEngineState.Collect)
            {
                return $"{_searchEngine.NodeCount:#,0} 個のインデックス作成中...";
            }
            else if (_searchEngine.State == SearchEngineState.Search)
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
