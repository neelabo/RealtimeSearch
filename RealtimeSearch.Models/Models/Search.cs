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
//using System.Windows.Threading;
//using System.Windows.Data;
using System.IO;
using System.Collections.Specialized;
using NeeLaboratory.IO.Search;
using NeeLaboratory.IO.Search.Files;
using NeeLaboratory.ComponentModel;

namespace NeeLaboratory.RealtimeSearch.Models
{
    public interface ISearchResultDecorator<T>
        where T : ISearchItem
    {
        void Decorate(SearchResult<T> searchResult);
    }

    public class Search : BindableBase
    {
        private int _busyCount;
        private bool _isBusy;
        private string _information = "";
        private readonly AppConfig _appConfig;
        //private readonly DispatcherTimer _timer;
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
            _searchEngine.SubscribePropertyChanged(nameof(FileSearchEngine.IsCollectBusy), SearchEngine_IsCollectBusyPropertyChanged);
            _searchEngine.SubscribePropertyChanged(nameof(FileSearchEngine.IsSearchBusy), SearchEngine_IsSearchBusyPropertyChanged);

            _appConfig.SearchAreas.CollectionChanged += SearchAreas_CollectionChanged;

            //_timer = new DispatcherTimer
            //{
            //    Interval = TimeSpan.FromMilliseconds(1000)
            //};
            //_timer.Tick += ProgressTimer_Tick;
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

        public bool IsCollectBusy => _searchEngine.IsCollectBusy;

        public bool IsSearchBusy => _searchEngine.IsSearchBusy;

        public string Information
        {
            get { return _information; }
            set { SetProperty(ref _information, value); }
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

        public ISearchResultDecorator<FileItem>? SearchResultDecorator { get; set; }


        private void SearchEngine_IsCollectBusyPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine($"State: IsCollectBusy={_searchEngine.IsCollectBusy}");
            UpdateInformation();
            RaisePropertyChanged(nameof(IsCollectBusy));
        }

        private void SearchEngine_IsSearchBusyPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine($"State: IsSearchBusy={_searchEngine.IsSearchBusy}");
            UpdateInformation();
            RaisePropertyChanged(nameof(IsSearchBusy));
        }

        private void SearchAreas_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ReIndex();
        }

        /// <summary>
        /// インデックス再構築
        /// </summary>
        public void ReIndex(FileForestMemento? memento = null)
        {
            var keyword = SearchResult?.Keyword;
            SearchResult = null;

            _searchEngine.SetSearchAreas(_appConfig.SearchAreas, memento);

            // 再検索
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                _ = SearchAsync(keyword, true);
            }
        }

        // TODO: Cache file name
        public FileForestMemento? LoadCache()
        {
            if (_appConfig.UseCache)
            {
                return FileForestCache.Load(FileForestCache.CacheFileName);
            }
            else
            {
                return null;
            }
        }

        public void SaveCache()
        {
            if (_appConfig.UseCache)
            {
                FileForestCache.Save(FileForestCache.CacheFileName, _searchEngine.Tree.CreateMemento());
            }
            else
            {
                FileForestCache.Remove(FileForestCache.CacheFileName);
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
                var keys = _searchEngine.Analyze(keyword);
                if (!keys.Any())
                {
                    return;
                }

                var searchResult = await _searchEngine.SearchAsync(keyword, _searchCancellationTokenSource.Token);
                if (searchResult.Exception != null)
                {
                    throw searchResult.Exception;
                }

                // 複数スレッドからコレクション操作できるようにする
                //BindingOperations.EnableCollectionSynchronization(searchResult.Items, new object());
                SearchResultDecorator?.Decorate(searchResult);

                SearchResult = new FileSearchResultWatcher(_searchEngine, searchResult);
                SearchResultChanged?.Invoke(this, EventArgs.Empty);


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

        //private void ProgressTimer_Tick(object? sender, EventArgs e)
        //{
        //    UpdateInformation();
        //    Debug.WriteLine($"Information = {Information}");
        //}

        public void UpdateInformation()
        {
            if (_searchEngine.IsSearchBusy)
            {
                Information = $"Searching...";
            }
            else if (_searchEngine.IsCollectBusy)
            {
                var indexing = $"{_searchEngine.Tree.Count} Indexing...";
                Information = string.IsNullOrEmpty(_message) ? indexing : $"{_message} ({indexing})";
            }
            else
            {
                Information = _message;
            }
        }

    }


}
