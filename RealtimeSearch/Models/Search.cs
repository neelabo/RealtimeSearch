using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Data;
using System.IO;
using System.Collections.Specialized;
using NeeLaboratory.IO.Search;
using NeeLaboratory.IO.Search.Files;

namespace NeeLaboratory.RealtimeSearch
{
    public class Search : BindableBase
    {
        private int _busyCount;
        private bool _isBusy;
        private string _information = "";
        private readonly AppConfig _appConfig;
        private readonly DispatcherTimer _timer;
        private readonly FileSearchEngine _searchEngine;
        private CancellationTokenSource _searchCancellationTokenSource = new();
        private FileSearchResultWatcher? _searchResult;
        private string _lastSearchKeyword = "";
        private string _message = "";


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Search(AppConfig appConfig)
        {
            _appConfig = appConfig;

            _searchEngine = new FileSearchEngine(_appConfig);
            _searchEngine.PropertyChanged += SearchEngine_PropertyChanged;

            _appConfig.SearchAreas.CollectionChanged += SearchAreas_CollectionChanged;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            _timer.Tick += ProgressTimer_Tick;
        }



        /// <summary>
        /// 結果変更イベント
        /// </summary>
        public EventHandler? SearchResultChanged;


        public bool IsBusy
        {
            get { return _isBusy; }
            set { SetProperty(ref _isBusy, value); }
        }
        public string Information
        {
            get { return _information; }
            set { _information = value; RaisePropertyChanged(); }
        }

        public FileSearchResultWatcher? SearchResult
        {
            get { return _searchResult; }
            set
            {
                if (_searchResult != value)
                {
                    _searchResult?.Dispose();
                    _searchResult = value; RaisePropertyChanged();
                }
            }
        }


        private void SearchEngine_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FileSearchEngine.State))
            {
                Debug.WriteLine($"State: {_searchEngine.State}");
                SetMessage("");
                _timer.IsEnabled = _searchEngine.State != SearchCommandEngineState.Idle;
                //IsBusyVisible = _searchEngine.State == SearchCommandEngineState.Search;
            }
        }

        private void SearchAreas_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ReIndex();
        }

        /// <summary>
        /// インデックス再構築
        /// </summary>
        public void ReIndex()
        {
            var keyword = SearchResult?.Keyword;
            SearchResult = null;

            _searchEngine.SetSearchAreas(_appConfig.SearchAreas);

            // 再検索
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                _ = SearchAsync(keyword, true);
            }
        }

        /// <summary>
        /// 特定パスの情報を更新
        /// </summary>
        public void Refresh(string path)
        {
            throw new NotImplementedException();
        }


        private void IncrementBusyCount()
        {
            IsBusy = Interlocked.Increment(ref _busyCount) > 0;
        }

        private void DecrementBusyCount()
        {
            IsBusy = Interlocked.Decrement(ref _busyCount) > 0;
        }

        /// <summary>
        /// 検索(非同期)
        /// </summary>
        public async Task SearchAsync(string keyword, bool isForce)
        {

            if (string.IsNullOrWhiteSpace(keyword))
            {
                _lastSearchKeyword = keyword;
                return;
            }

            if (!isForce && keyword == _lastSearchKeyword)
            {
                return;
            }

            _lastSearchKeyword = keyword;

            // 同時に実行可能なのは1検索のみ。以前の検索はキャンセルして新しい検索コマンドを発行
            _searchCancellationTokenSource.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();

            IncrementBusyCount();
            try
            {
                SetMessage("");

                // キーワード検証
                _searchEngine.Analyze(keyword);

                var searchResult = await _searchEngine.SearchAsync(keyword, _searchCancellationTokenSource.Token);
                if (searchResult.Exception != null)
                {
                    throw searchResult.Exception;
                }

                SearchResult = new FileSearchResultWatcher(_searchEngine, searchResult);
                SearchResultChanged?.Invoke(this, EventArgs.Empty);

                // 複数スレッドからコレクション操作できるようにする
                BindingOperations.EnableCollectionSynchronization(SearchResult.Items, new object());

                SetMessage($"{SearchResult.Items.Count:#,0} 個の項目");

                // 項目変更監視
                SearchResult.Items.CollectionChanged += (s, e) => SearchResultChanged?.Invoke(s, e);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"Search Canceled: {keyword}");
                //Information = "";
            }
            catch (SearchKeywordException e)
            {
                if (e is SearchKeywordRegularExpressionException ex1)
                {
                    SetMessage("正規表現エラー: " + ex1.InnerException?.Message);
                }
                else if (e is SearchKeywordDateTimeException ex2)
                {
                    SetMessage("日時指定が不正です");
                }
                else if (e is SearchKeywordBooleanException ex3)
                {
                    SetMessage("フラグ指定が不正です");
                }
                else if (e is SearchKeywordOptionException ex4)
                {
                    SetMessage("不正なオプションです: " + ex4.Option);
                }
                else
                {
                    SetMessage(e.Message);
                }
            }
            catch (Exception e)
            {
                SetMessage(e.Message);
                throw;
            }
            finally
            {
                DecrementBusyCount();
            }
        }


        private void SetMessage(string message)
        {
            _message = message;
            UpdateInformation();
        }

        private void ProgressTimer_Tick(object? sender, EventArgs e)
        {
            UpdateInformation();
            Debug.WriteLine($"Information = {Information}");
        }

        private void UpdateInformation()
        {
            Information = _searchEngine.State switch
            {
                SearchCommandEngineState.Idle
                    => _message,
                SearchCommandEngineState.Collect
                    => $"Indexing... ({_searchEngine.Tree.Count})",
                SearchCommandEngineState.Search
                    => $"Searching...",
                _
                    => ""
            }; ;
        }

    }


}
