// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealtimeSearch
{
    public enum SearchEngineState
    {
        None,
        Search, // 処理中です。
        SearchResult,
        SearchResultEmpty, // "条件に一致する項目はありません。";
    }


    public class SearchEngine : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion


        //
        public event EventHandler<SearchEngineState> StateMessageChanged;

        // 状態
        #region Property: State
        private SearchEngineState _State;
        public SearchEngineState State
        {
            get { return _State; }
            set
            {
                if (_State != value)
                {
                    _State = value;
                    OnPropertyChanged();
                    OnPropertyChanged("StateMessage");
                    App.Current.Dispatcher.Invoke(() => StateMessageChanged?.Invoke(this, _State));
                }
            }
        }
        #endregion


        // 状態メッセージ(表示用)
        public string StateMessage
        {
            get
            {
                if (_State == SearchEngineState.Search)
                    return "処理中です...";
                else if (_State == SearchEngineState.SearchResultEmpty)
                    return "条件に一致する項目はありません。";
                else
                    return null;
            }
        }


        // 現在のコマンド
        #region Property: Command
        private SearchEngineCommand _Command;
        public SearchEngineCommand Command
        {
            get { return _Command; }
            set { _Command = value; OnPropertyChanged(); }
        }
        #endregion


        // 現在のコマンド予約数
        #region Property: CommandCount
        private int _CommandCount;
        public int CommandCount
        {
            get { return _CommandCount; }
            set { _CommandCount = value; OnPropertyChanged(); }
        }
        #endregion


        // コマンドリスト
        private List<SearchEngineCommand> _CommandList;

        // コマンドの製造番号用カウンタ
        private int _CommandSerialNumber;

        // エンジンとなるタスク
        private Task _Task;

        // コマンド数セマフォ。エンジンタスク駆動に利用
        private SemaphoreSlim _CommandSemaphore;

        // 排他処理用ロック
        private Object _Lock = new Object();

        // 検索コア
        private SearchCore _SearchCore;

        // 検索結果に対応している現在のキーワード
        public string SearchKeyword { get; private set; }

        // 検索結果に対応しているフォルダオプション
        public bool IsSearchFolder { get; private set; }

        // 検索結果
        public ObservableCollection<File> SearchResult { get { return _SearchCore.SearchResult; } }

        // 検索結果の変更イベント
        public event EventHandler<int> ResultChanged;

        // 外部からコマンドをリクエストできるようにグローバルインスタンスを公開
        public static SearchEngine Current { get; private set; }



        public SearchEngine()
        {
            Current = this;

            _SearchCore = new SearchCore();
            _CommandSemaphore = new SemaphoreSlim(0);
            _CommandList = new List<SearchEngineCommand>();
        }


        public void Start()
        {
            _Task = Task.Run(() => EngineAsync());
        }


        private void AddCommand(SearchEngineCommand command)
        {
            command.SearchEngine = this;
            command.SerialNumber = _CommandSerialNumber++;
            _CommandList.Add(command);
            CommandCount = _CommandList.Count;

            _CommandSemaphore.Release();
        }


        // インデックス化リクエスト
        public void IndexRequest(string[] paths)
        {
            lock (_Lock)
            {
                _CommandList.ForEach(cmd => { if (cmd is IndexCommand || cmd is ReIndexCommand) cmd.IsCancel = true; });

                AddCommand(new IndexCommand() { Paths = paths });
            }
        }


        // 再インデックス化リクエスト
        public void ReIndexRequest()
        {
            lock (_Lock)
            {
                if (_CommandList.Any(cmd => cmd is IndexCommand || cmd is ReIndexCommand)) return;

                AddCommand(new ReIndexCommand());
            }
        }


        // パス追加リクエスト
        public void AddIndexRequest(string root, string path)
        {
            lock (_Lock)
            {
                if (_CommandList.Any(cmd => cmd is IndexCommand || cmd is ReIndexCommand)) return;

                // コマンドをまとめる
                AddIndexCommand command = (_CommandList.Count >= 1) ? _CommandList.LastOrDefault(c => c is AddIndexCommand && ((AddIndexCommand)c).Root == root) as AddIndexCommand : null;
                if (command != null && command.Root == root)
                {
                    command.Paths.Add(path);
                }
                else
                {
                    command = new AddIndexCommand() { Root = root };
                    command.Paths.Add(path);

                    AddCommand(command);
                }
            }
        }


        // パス削除リクエスト
        public void RemoveIndexRequest(string root, string path)
        {
            lock (_Lock)
            {
                if (_CommandList.Any(cmd => cmd is IndexCommand || cmd is ReIndexCommand)) return;

                AddCommand(new RemoveIndexCommand() { Root = root, Path = path });
            }
        }


        // リネームリクエスト
        public void RenameIndexRequest(string root, string oldPath, string path)
        {
            lock (_Lock)
            {
                if (_CommandList.Any(cmd => cmd is SearchCommand)) return;
                AddCommand(new RenameIndexCommand() { Root = root, OldPath = oldPath, Path = path });
            }
        }


        // 検索リクエスト
        public void SearchRequest(string keyword, bool isSearchFolder)
        {
            keyword = keyword ?? "";
            keyword = keyword.Trim();

            lock (_Lock)
            {
                _CommandList.ForEach(cmd => { if (cmd is SearchCommand) cmd.IsCancel = true; });

                // 他のコマンドが存在する場合のみメッセージ更新
                // 制限する理由は、ちらつき防止のため
                if (Command != null || _CommandList.Count > 1)
                {
                    ResultChanged?.Invoke(this, 0);
                    State = string.IsNullOrEmpty(keyword) ? SearchEngineState.None : SearchEngineState.Search;
                }

                AddCommand(new SearchCommand() { Keyword = keyword, IsSearchFolder = isSearchFolder });
            }
        }



        //
        // ワーカータスク
        public async Task EngineAsync()
        {
            while (true)
            {
                // コマンドがあることをSemaphoreで検知する
                await _CommandSemaphore.WaitAsync();

                lock (_Lock)
                {
                    // コマンド取り出し
                    Command = _CommandList[0];
                    _CommandList.RemoveAt(0);

                    CommandCount = _CommandList.Count;
                }

                if (Command.IsCancel) continue;

#if DEBUG
                await Task.Delay(100); // 開発用遅延
#endif
                try
                {
                    Command.Exec();
                }
                catch (Exception e)
                {
                    // エラーはスルー
                    Debug.WriteLine(e.Message);
                }

                Command = null;
            }
        }


        public void CommandIndex(string[] paths)
        {
            _SearchCore.Collect(paths);
        }

        public void CommandReIndex()
        {
            _SearchCore.Collect();
        }

        public void CommandAddIndex(string root, List<string> paths)
        {
            List<File> newFiles = _SearchCore.AddPaths(root, paths);

            var items = _SearchCore.Search(SearchKeyword, newFiles, IsSearchFolder);
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var file in items)
                {
                    SearchResult.Add(file);
                }
            });
        }

        public void CommandRemoveIndex(string root, string path)
        {
            List<File> removeFiles = _SearchCore.RemovePath(root, path);

            var items = SearchResult.Where(f => removeFiles.Any(e => e.Path == f.Path)).ToList();
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (File item in items)
                {
                    SearchResult.Remove(item);
                }
            });
        }

        public void CommandRenameIndex(string root, string oldFileName, string fileName)
        {
            List<File> removeFiles = _SearchCore.RemovePath(root, oldFileName);
            List<File> newFiles = _SearchCore.AddPaths(root, new List<string>() { fileName });

            // リネーム保持要求がある場合はマッチングにかかわらずなるべく項目をそのままにする
            var removeFileOne = removeFiles.Find(f => f.Path == oldFileName);
            var newFileOne = newFiles.Find(f => f.Path == fileName);
            if (removeFileOne.IsKeep)
            {
                removeFileOne.IsKeep = false;
                if (removeFileOne != null && newFileOne != null)
                {
                    bool isChanged = ChangeResultOne(removeFileOne, newFileOne);
                    if (isChanged)
                    {
                        removeFiles.Remove(removeFileOne);
                        newFiles.Remove(newFileOne);
                    }
                }
            }

            // 差し替え
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (File item in SearchResult.Where(f => removeFiles.Any(e => e.Path == f.Path)).ToList())
                {
                    SearchResult.Remove(item);
                }
                foreach (var file in _SearchCore.Search(SearchKeyword, newFiles, IsSearchFolder))
                {
                    SearchResult.Add(file);
                }
            });
        }

        // 項目入れ替え
        private bool ChangeResultOne(File oldFile, File newFile)
        {
            var item = SearchResult?.FirstOrDefault(e => e.Path == oldFile.Path);
            if (item != null)
            {
                int index = SearchResult.IndexOf(item);
                App.Current.Dispatcher.Invoke(() => SearchResult[index] = newFile);
                return true;
            }
            else
            {
                return false;
            }
        }


        public void CommandSearch(string keyword, bool isSearchFolder)
        {
            _SearchCore.UpdateSearchResult(keyword, isSearchFolder);

            lock (_Lock)
            {
                if (_CommandList.Any(n => n is SearchCommand))
                {
                    State = SearchEngineState.Search;
                }
                else if (_SearchCore.SearchResult.Count <= 0)
                {
                    State = string.IsNullOrEmpty(keyword) ? SearchEngineState.None : SearchEngineState.SearchResultEmpty;
                    SearchKeyword = keyword;
                    IsSearchFolder = isSearchFolder;
                    ResultChanged?.Invoke(this, _SearchCore.SearchResult.Count);
                }
                else
                {
                    State = SearchEngineState.SearchResult;
                    SearchKeyword = keyword;
                    IsSearchFolder = isSearchFolder;
                    ResultChanged?.Invoke(this, _SearchCore.SearchResult.Count);
                }
            }
        }
    }
}
