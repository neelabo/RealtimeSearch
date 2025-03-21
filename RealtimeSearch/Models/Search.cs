﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Collections.Specialized;
using NeeLaboratory.IO.Search;
using NeeLaboratory.IO.Search.Files;
using NeeLaboratory.RealtimeSearch.Services;
using NeeLaboratory.RealtimeSearch.TextResource;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NeeLaboratory.RealtimeSearch.Models
{
    public interface ISearchResultDecorator<T>
        where T : ISearchItem
    {
        void Decorate(SearchResult<T> searchResult);
    }

    public class Search : ObservableObject, IDisposable
    {
        private int _busyCount;
        private bool _isBusy;
        private string _information = "";
        private readonly AppSettings _settings;
        private readonly FileSearchEngine _searchEngine;
        private CancellationTokenSource _searchCancellationTokenSource = new();
        private FileSearchResultWatcher? _searchResult;
        private string _lastSearchKeyword = "";
        private string _message = "";
        private bool _disposedValue;


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Search(AppSettings settings)
        {
            _settings = settings;

            _searchEngine = new FileSearchEngine(_settings);
            _searchEngine.SubscribePropertyChanged(nameof(FileSearchEngine.IsCollectBusy), SearchEngine_IsCollectBusyPropertyChanged);
            _searchEngine.SubscribePropertyChanged(nameof(FileSearchEngine.IsSearchBusy), SearchEngine_IsSearchBusyPropertyChanged);

            _settings.SearchAreas.CollectionChanged += SearchAreas_CollectionChanged;
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
                    _searchResult = value;
                    OnPropertyChanged();
                }
            }
        }

        public ISearchResultDecorator<FileContent>? SearchResultDecorator { get; set; }


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _searchCancellationTokenSource.Cancel();
                    _searchCancellationTokenSource.Dispose();
                    _searchResult?.Dispose();
                    _searchEngine.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void SearchEngine_IsCollectBusyPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine($"State: IsCollectBusy={_searchEngine.IsCollectBusy}");
            UpdateInformation();
            OnPropertyChanged(nameof(IsCollectBusy));
        }

        private void SearchEngine_IsSearchBusyPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine($"State: IsSearchBusy={_searchEngine.IsSearchBusy}");
            UpdateInformation();
            OnPropertyChanged(nameof(IsSearchBusy));
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

            _searchEngine.SetSearchAreas(_settings.SearchAreas, memento);

            // 再検索
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                _ = SearchAsync(keyword, true);
            }
        }

        // TODO: Cache file name
        public FileForestMemento? LoadCache()
        {
            if (_settings.UseCache)
            {
                var fileName = GetCacheFileName();
                return FileForestCache.Load(fileName);
            }
            else
            {
                return null;
            }
        }

        public void SaveCache()
        {
            var fileName = GetCacheFileName();
            if (_settings.UseCache)
            {
                FileForestCache.Save(fileName, _searchEngine.Tree.CreateMemento());
            }
            else
            {
                FileForestCache.Remove(fileName);
            }
        }

        private string GetCacheFileName()
        {
            return System.IO.Path.Combine(ApplicationInfo.Current.LocalApplicationDataPath, FileForestCache.CacheFileName);
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
            var cts = _searchCancellationTokenSource;
            _searchCancellationTokenSource = new CancellationTokenSource();
            cts.Cancel();
            cts.Dispose();

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

                SetMessage("");

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
                    SetMessage(ResourceService.GetString("@Status.RegexError") + ": " + ex1.InnerException?.Message);
                }
                else if (e is SearchKeywordDateTimeException ex2)
                {
                    SetMessage(ResourceService.GetString("@Status.DateTimeError"));
                }
                else if (e is SearchKeywordBooleanException ex3)
                {
                    SetMessage(ResourceService.GetString("@Status.FlagError"));
                }
                else if (e is SearchKeywordOptionException ex4)
                {
                    SetMessage(ResourceService.GetString("@Status.OptionError") + ": " + ex4.Option);
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

        public void UpdateInformation()
        {
            if (_searchEngine.IsSearchBusy)
            {
                Information = ResourceService.GetString("@Status.Searching");
            }
            else if (_searchEngine.IsCollectBusy)
            {
                var indexing = ResourceService.GetFormatString("@Status.Indexing", _searchEngine.Tree.Count);
                Information = string.IsNullOrEmpty(_message) ? indexing : $"{_message} ({indexing})";
            }
            else
            {
                Information = _message;
            }
        }

    }


}
