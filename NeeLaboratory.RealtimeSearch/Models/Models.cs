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
        /// 結果変更イベント
        /// </summary>
        public EventHandler SearchResultChanged;


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
            set
            {
                if (_IsBusy != value)
                {
                    _IsBusy = value;

                    if (_IsBusy)
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

        /// <summary>
        /// IsBusyVisibled property.
        /// </summary>
        private bool _IsBusyVisibled;
        public bool IsBusyVisibled
        {
            get { return _IsBusyVisibled; }
            set { if (_IsBusyVisibled != value) { _IsBusyVisibled = value; RaisePropertyChanged(); } }
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


        private DispatcherTimer _timer;

        /// <summary>
        /// コンストラクタ
        /// TODO: 設定をわたしているが、設定の読込もここでしょ
        /// </summary>
        /// <param name="setting"></param>
        public Models(Setting setting)
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(1000);
            _timer.Tick += ProgressTimer_Tick;

            _setting = setting;

            _searchEngine = new SearchEngine();
            ////_searchEngine.Context.NodeFilter = SearchFilter;
            _searchEngine.SetSearchAreas(_setting.SearchAreas);
            _searchEngine.Start();

            //SearchEngine.Logger.SetLevel(SourceLevels.All);
            //_searchEngine.CommandEngineLogger.SetLevel(SourceLevels.All);
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

        /// <summary>
        /// インデックス再構築
        /// </summary>
        public void ReIndex()
        {
            _searchEngine.SetSearchAreas(_setting.SearchAreas);
        }


        /// <summary>
        /// 特定パスの情報を更新
        /// </summary>
        /// <param name="path"></param>
        public void Reflesh(string path)
        {
            _searchEngine.Reflesh(path);
        }

        /// <summary>
        /// 名前変更
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        public void Rename(string src, string dst)
        {
            _searchEngine.Rename(src, dst);
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

        //
        private SearchResultWatcher _watcher;

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
                IsBusy = true;

                // 同時に実行可能なのは1検索のみ。以前の検索はキャンセルして新しい検索コマンドを発行
                _searchCancellationTokenSource?.Cancel();
                _searchCancellationTokenSource = new CancellationTokenSource();
                var searchResult = await _searchEngine.SearchAsync(keyword, _setting.SearchOption, _searchCancellationTokenSource.Token);
                if (searchResult.Exception != null)
                {
                    throw searchResult.Exception;
                }

                SearchResult = searchResult;
                SearchResultChanged?.Invoke(this, null);

                // 複数スレッドからコレクション操作できるようにする
                BindingOperations.EnableCollectionSynchronization(SearchResult.Items, new object());

                Information = $"{SearchResult.Items.Count:#,0} 個の項目";

                // 項目変更監視
                SearchResult.Items.CollectionChanged += (s, e) => SearchResultChanged?.Invoke(s, e);

                // 監視開始
                _watcher?.Dispose();
                _watcher = new SearchResultWatcher(_searchEngine, SearchResult);
                _watcher.Start();
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

        //
        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            Information = GetSearchEngineProgress();
            IsBusyVisibled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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

    }


}
