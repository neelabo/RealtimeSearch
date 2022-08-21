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
using System.Windows.Threading;
using System.Windows.Data;
using System.IO;
using System.Collections.Specialized;

namespace NeeLaboratory.RealtimeSearch
{
    public class Search : BindableBase
    {
        private bool _isBusy;
        private bool _isBusyVisibled;
        private string _information = "";
        private AppConfig _appConfig;
        private DispatcherTimer _timer;
        private SearchEngine _searchEngine;
        private SearchResult? _searchResult;
        private CancellationTokenSource _searchCancellationTokenSource = new CancellationTokenSource();
        private SearchResultWatcher? _watcher;
        private string _lastSearchKeyword = "";


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Search(AppConfig appConfig)
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(1000);
            _timer.Tick += ProgressTimer_Tick;

            _appConfig = appConfig;
            _appConfig.SearchAreas.CollectionChanged += SearchAreas_CollectionChanged;

            _searchEngine = new SearchEngine();
            ////_searchEngine.Context.NodeFilter = SearchFilter;
            _searchEngine.SetSearchAreas(_appConfig.SearchAreas);
        }


        /// <summary>
        /// 結果変更イベント
        /// </summary>
        public EventHandler? SearchResultChanged;


        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;

                    if (_isBusy)
                    {
                        _timer.Start();
                    }
                    else
                    {
                        _timer.Stop();
                        IsBusyVisibled = false;
                    }

                    RaisePropertyChanged();
                }
            }
        }

        public bool IsBusyVisibled
        {
            get { return _isBusyVisibled; }
            set { if (_isBusyVisibled != value) { _isBusyVisibled = value; RaisePropertyChanged(); } }
        }

        public string Information
        {
            get { return _information; }
            set { _information = value; RaisePropertyChanged(); }
        }

        public SearchResult? SearchResult
        {
            get { return _searchResult; }
            set { if (_searchResult != value) { _searchResult = value; RaisePropertyChanged(); } }
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
            _searchEngine.SetSearchAreas(_appConfig.SearchAreas);
        }

        /// <summary>
        /// 特定パスの情報を更新
        /// </summary>
        public void Reflesh(string path)
        {
            _searchEngine.Reflesh(path);
        }

        /// <summary>
        /// 名前変更
        /// </summary>
        public void Rename(string src, string dst)
        {
            _searchEngine.Rename(src, dst);
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

            try
            {
                IsBusy = true;

                // 同時に実行可能なのは1検索のみ。以前の検索はキャンセルして新しい検索コマンドを発行
                _searchCancellationTokenSource.Cancel();
                _searchCancellationTokenSource = new CancellationTokenSource();
                var searchResult = await _searchEngine.SearchAsync(keyword, _appConfig.SearchOption, _searchCancellationTokenSource.Token);
                if (searchResult.Exception != null)
                {
                    throw searchResult.Exception;
                }

                SearchResult = searchResult;
                SearchResultChanged?.Invoke(this, EventArgs.Empty);

                // 複数スレッドからコレクション操作できるようにする
                BindingOperations.EnableCollectionSynchronization(SearchResult.Items, new object());

                Information = $"{SearchResult.Items.Count:#,0} 個の項目";

                // 項目変更監視
                SearchResult.Items.CollectionChanged += (s, e) => SearchResultChanged?.Invoke(s, e);

                // 監視開始
                _watcher?.Dispose();
                _watcher = new SearchResultWatcher(_searchEngine, SearchResult);
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
                    Information = "正規表現エラー: " + ex1.InnerException?.Message;
                }
                if (e is SearchKeywordDateTimeException ex2)
                {
                    Information = "日時指定が不正です";
                }
                else if (e is SearchKeywordOptionException ex3)
                {
                    Information = "不正なオプションです: " + ex3.Option;
                }
                else
                {
                    Information = e.Message;
                }
            }
            catch (Exception e)
            {
                Information = e.Message;
                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ProgressTimer_Tick(object? sender, EventArgs e)
        {
            Information = GetSearchEngineProgress();
            IsBusyVisibled = true;
        }

        private string GetSearchEngineProgress()
        {
            if (_searchEngine.State == SearchCommandEngineState.Collect)
            {
                return $"{_searchEngine.NodeCountMaybe:#,0} 個のインデックス作成中...";
            }
            else if (_searchEngine.State == SearchCommandEngineState.Search)
            {
                return $"検索中...";
            }
            else
            {
                return $"処理中...";
            }
        }

#if false // フィルターサンプル

        /// <summary>
        /// インデックスフィルタ用無効属性
        /// </summary>
        private static FileAttributes _ignoreAttributes =  FileAttributes.ReparsePoint | FileAttributes.Hidden | FileAttributes.System | FileAttributes.Temporary;

        /// <summary>
        /// インデックスフィルタ用無効パス
        /// </summary>
        private static List<string> _ignores = new List<string>()
        {
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows),
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows) + ".old",
        };

        /// <summary>
        /// インデックスフィルタ
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private static bool SearchFilter(FileSystemInfo info)
        {
            // 属性フィルター
            if ((info.Attributes & _ignoreAttributes) != 0)
            {
                return false;
            }

            // ディレクトリ無効フィルター
            if ((info.Attributes & FileAttributes.Directory) != 0)
            {
                var infoFullName = info.FullName;
                var infoLen = infoFullName.Length;

                foreach (var ignore in _ignores)
                {
                    var ignoreLen = ignore.Length;

                    if (ignoreLen == infoLen || (ignoreLen < infoLen && infoFullName[ignoreLen] == '\\'))
                    {
                        if (infoFullName.StartsWith(ignore, true, null))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

#endif
    }


}
