﻿// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
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

namespace RealtimeSearch.Search
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

        //
        public event EventHandler<string> IndexCountChanged;


        // 状態
        #region Property: State
        private SearchEngineState _state;
        public SearchEngineState State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged();
                    OnPropertyChanged("StateMessage");
                    App.Current.Dispatcher.Invoke(() => StateMessageChanged?.Invoke(this, _state));
                }
            }
        }
        #endregion


        // 状態メッセージ(表示用)
        public string StateMessage
        {
            get
            {
                if (_state == SearchEngineState.Search)
                    return "処理中です";
                else if (_state == SearchEngineState.SearchResultEmpty)
                    return "条件に一致する項目はありません。";
                else
                    return null;
            }
        }


        // 現在のコマンド
        #region Property: Command
        private SearchEngineCommand _command;
        public SearchEngineCommand Command
        {
            get { return _command; }
            set { _command = value; OnPropertyChanged(); }
        }
        #endregion


        // 現在のコマンド予約数
        #region Property: CommandCount
        private int _commandCount;
        public int CommandCount
        {
            get { return _commandCount; }
            set { _commandCount = value; OnPropertyChanged(); }
        }
        #endregion


        // コマンドリスト
        private List<SearchEngineCommand> _commandList;

        // コマンドの製造番号用カウンタ
        private int _commandSerialNumber;

        // エンジンとなるタスク
        private Task _task;

        // コマンド数セマフォ。エンジンタスク駆動に利用
        private SemaphoreSlim _commandSemaphore;

        // 排他処理用ロック
        private Object _lock = new Object();

        // 検索コア
        private SearchCore _searchCore;

        // 検索結果に対応している現在のキーワード
        #region Property: SearchKeyword
        private string _searchKeyword = "";
        public string SearchKeyword
        {
            get { return _searchKeyword; }
            private set { _searchKeyword = value; OnPropertyChanged(); }
        }
        #endregion


        // 検索結果に対応している検索オプション
        public SearchOption SearchOption { get; private set; }


        // 検索結果
        public ObservableCollection<NodeContent> SearchResult { get { return _searchCore.SearchResult; } }

        // 検索結果の変更イベント
        public event EventHandler ResultChanged;


        // 外部からコマンドをリクエストできるようにグローバルインスタンスを公開
        public static SearchEngine Current { get; private set; }



        public SearchEngine()
        {
            Current = this;

            _searchCore = new SearchCore();
            _commandSemaphore = new SemaphoreSlim(0);
            _commandList = new List<SearchEngineCommand>();
        }


        public void Start()
        {
            _task = Task.Run(() => EngineAsync());
        }


        private void AddCommand(SearchEngineCommand command)
        {
            command.SearchEngine = this;
            command.SerialNumber = _commandSerialNumber++;
            _commandList.Add(command);
            CommandCount = _commandList.Count;

            _commandSemaphore.Release();
        }


        // インデックス化リクエスト
        public void IndexRequest(string[] paths)
        {
            lock (_lock)
            {
                _commandList.ForEach(cmd => { if (cmd is IndexCommand || cmd is ReIndexCommand) cmd.IsCancel = true; });

                AddCommand(new IndexCommand() { Paths = paths });
            }
        }


        // 再インデックス化リクエスト
        public void ReIndexRequest()
        {
            lock (_lock)
            {
                if (_commandList.Any(cmd => cmd is IndexCommand || cmd is ReIndexCommand)) return;

                AddCommand(new ReIndexCommand());
            }
        }


        // パス追加リクエスト
        public void AddIndexRequest(string root, string path)
        {
            lock (_lock)
            {
                if (_commandList.Any(cmd => cmd is IndexCommand || cmd is ReIndexCommand)) return;

                // コマンドをまとめる？
                AddIndexCommand command = (_commandList.Count >= 1) ? _commandList.LastOrDefault(c => c is AddIndexCommand && ((AddIndexCommand)c).Root == root) as AddIndexCommand : null;
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
            lock (_lock)
            {
                if (_commandList.Any(cmd => cmd is IndexCommand || cmd is ReIndexCommand)) return;

                AddCommand(new RemoveIndexCommand() { Root = root, Path = path });
            }
        }


        // リネームリクエスト
        public void RenameIndexRequest(string root, string oldPath, string path)
        {
            lock (_lock)
            {
                if (_commandList.Any(cmd => cmd is IndexCommand || cmd is ReIndexCommand)) return;
                AddCommand(new RenameIndexCommand() { Root = root, OldPath = oldPath, Path = path });
            }
        }


        // 情報更新リクエスト
        public void RefleshIndexRequest(string root, string path)
        {
            lock (_lock)
            {
                if (_commandList.Any(cmd => cmd is IndexCommand || cmd is ReIndexCommand)) return;
                AddCommand(new RefleshIndexCommand() { Root = root, Path = path });
            }
        }


        // 検索リクエスト
        public void SearchRequest(string keyword, SearchOption option)
        {
            keyword = keyword ?? "";
            keyword = keyword.Trim();

            lock (_lock)
            {
                _commandList.ForEach(cmd => { if (cmd is SearchCommand) cmd.IsCancel = true; });

                // 他のコマンドが存在する場合のみメッセージ更新
                // 制限する理由は、ちらつき防止のため
                if (Command != null || _commandList.Count > 1)
                {
                    ResultChanged?.Invoke(this, null);
                    State = string.IsNullOrEmpty(keyword) ? SearchEngineState.None : SearchEngineState.Search;
                }

                AddCommand(new SearchCommand() { Keyword = keyword, Option = option });
            }
        }



        //
        // ワーカータスク
        public async Task EngineAsync()
        {
            while (true)
            {
                // コマンドがあることをSemaphoreで検知する
                await _commandSemaphore.WaitAsync();

                lock (_lock)
                {
                    // コマンド取り出し
                    Command = _commandList[0];
                    _commandList.RemoveAt(0);

                    CommandCount = _commandList.Count;
                }

                if (Command.IsCancel) continue;

#if DEBUG
                await Task.Delay(100); // 開発用遅延
#endif
                try
                {
#if DEBUG
                    var sw = new Stopwatch();
                    sw.Start();
                    Command.Exec();
                    sw.Stop();
                    Debug.WriteLine($"({sw.ElapsedMilliseconds}ms) {Command}");
#else
                    Command.Exec();
#endif
                }
                catch (Exception e)
                {
                    // エラーはスルー
                    Debug.WriteLine(e.Message);
                }

                Command = null;
            }
        }


        //
        private void SendIndexCountChanged(string text)
        {
            App.Current.Dispatcher.Invoke(() => IndexCountChanged?.Invoke(this, text));
        }

        //
        private void CreateIndex(Action action)
        {
            var tokenSource = new CancellationTokenSource();
            Task.Run(async () =>
            {
                await Task.Delay(200);
                while (!tokenSource.Token.IsCancellationRequested)
                {
                    SendIndexCountChanged($"{Node.TotalCount:#,0} 個のインデックス作成中");
                    await Task.Delay(1000);
                }
                SendIndexCountChanged("");
            });

            action();

            tokenSource.Cancel();
        }


        public void CommandIndex(string[] paths)
        {
            CreateIndex(() => _searchCore.Collect(paths));
        }

        public void CommandReIndex()
        {
            CreateIndex(() => _searchCore.Collect());
        }

        public void CommandAddIndex(string root, List<string> paths)
        {
            foreach (var path in paths)
            {
                Node node = _searchCore.AddPath(root, path);
                if (node != null)
                {
                    var items = _searchCore.Search(SearchKeyword, node.AllNodes, SearchOption);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var file in items)
                        {
                            SearchResult.Add(file.Content);
                        }
                    });

                    UpdateSearchResultState();
                }
            }
        }

        public void CommandRemoveIndex(string root, string path)
        {
            Node node = _searchCore.RemovePath(root, path);

            var items = SearchResult.Where(f => f.IsRemoved).ToList();
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var item in items)
                {
                    SearchResult.Remove(item);
                }
            });

            UpdateSearchResultState();
        }

        public void CommandRenameIndex(string root, string oldFileName, string fileName)
        {
            var node = _searchCore.RenamePath(root, oldFileName, fileName);
            if (node != null && !SearchResult.Contains(node.Content))
            {
                var items = _searchCore.Search(SearchKeyword, new List<Node>() { node }, SearchOption);

                if (items.Count() > 0)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var file in items)
                        {
                            SearchResult.Add(file.Content);
                        }
                    });

                    UpdateSearchResultState();
                }
            }
        }


        // ファイル情報更新
        public void CommandRefleshIndex(string root, string path)
        {
            _searchCore.RefleshIndex(root, path);
        }



        // 検索
        public void CommandSearch(string keyword, SearchOption option)
        {
            _searchCore.UpdateSearchResult(keyword, option);

            lock (_lock)
            {
                SearchKeyword = keyword;
                SearchOption = option.Clone();
                UpdateSearchResultState();
            }
        }

        // 検索結果状態更新
        private void UpdateSearchResultState()
        {
            if (_commandList.Any(n => n is SearchCommand))
            {
                //State = SearchEngineState.Search;
            }
            else if (SearchResult.Count <= 0)
            {
                State = string.IsNullOrEmpty(SearchKeyword) ? SearchEngineState.None : SearchEngineState.SearchResultEmpty;
            }
            else
            {
                State = SearchEngineState.SearchResult;
            }

            ResultChanged?.Invoke(this, null);
        }
    }
}
