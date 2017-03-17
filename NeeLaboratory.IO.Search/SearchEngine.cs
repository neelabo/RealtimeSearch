using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeeLaboratory.IO.Search
{
    public class SearchEngine : IDisposable
    {
        private ObservableCollection<string> _searchAreas;
        public ObservableCollection<string> SearchAreas
        {
            get => _searchAreas;
            set
            {
                if (_searchAreas != value)
                {
                    if (_searchAreas != null)
                    {
                        _searchAreas.CollectionChanged -= Areas_CollectionChanged;
                    }
                    _searchAreas = value;
                    if (_searchAreas != null)
                    {
                        _searchAreas.CollectionChanged += Areas_CollectionChanged;
                    }
                    ResetArea();
                }
            }
        }

        public void SetSearchAreas(IEnumerable<string> areas)
        {
            this.SearchAreas = new ObservableCollection<string>(areas);
        }

        private SearchCore _core;
        internal SearchCore Core => _core;

        public SearchEngineState State => _commandEngine.State;

        public int NodeCount => _core.NodeCount();

        private SerarchCommandEngine _commandEngine;


        public SearchEngine()
        {
            this.SearchAreas = new ObservableCollection<string>();

            _core = new SearchCore();
            _core.FileSystemChanged += Core_FileSystemChanged;
        }

        //
        private void Core_FileSystemChanged(object sender, NodeTreeFileSystemEventArgs e)
        {
            switch (e.FileSystemEventArgs.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    AddIndex(e.NodePath, e.FileSystemEventArgs.FullPath);
                    break;
                case WatcherChangeTypes.Deleted:
                    RemoveIndex(e.NodePath, e.FileSystemEventArgs.FullPath);
                    break;
                case WatcherChangeTypes.Renamed:
                    var rename = e.FileSystemEventArgs as RenamedEventArgs;
                    RenameIndex(e.NodePath, rename.OldFullPath, rename.FullPath);
                    break;
                case WatcherChangeTypes.Changed:
                    RefleshIndex(e.NodePath, e.FileSystemEventArgs.FullPath);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        //
        public void Start()
        {
            _commandEngine = new SerarchCommandEngine();
            _commandEngine.Initialize();

            if (_searchAreas != null) ResetArea();
        }

        public void Stop()
        {
            _commandEngine.Dispose();
            _commandEngine = null;
        }

        public async Task WaitAsync()
        {
            if (_commandEngine == null) throw new InvalidOperationException("engine stopped.");

            var command = new WaitCommand(this, null);
            _commandEngine.Enqueue(command);

            await command.WaitAsync();
        }


        //
        private void Areas_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Reset:
                    ResetArea();
                    break;

                default:
                    throw new NotImplementedException("not support yet.");
            }
        }



        /// <summary>
        /// 検索範囲の再構築
        /// </summary>
        private void ResetArea()
        {
            if (_commandEngine == null) return;

            var command = new ResetAreaCommand(this, new ResetAreaCommandArgs() { Area = _searchAreas?.ToArray() });
            _commandEngine.Enqueue(command);
        }


        internal void ResetArea_Execute(ResetAreaCommandArgs args)
        {
            _core.Collect(args.Area);
            Debug.WriteLine($"NodeCount = {_core.NodeCount()}");
        }

        /// <summary>
        /// 検索
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public async Task<SearchResult> SearchAsync(string keyword, SearchOption option)
        {
            return await SearchAsync(keyword, option, CancellationToken.None);
        }

        /// <summary>
        /// 検索
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="option"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<SearchResult> SearchAsync(string keyword, SearchOption option, CancellationToken token)
        {
            if (_commandEngine == null) throw new InvalidOperationException("engine stopped.");

            var command = new SearchCommand(this, new SearchExCommandArgs() { Keyword = keyword, Option = option });
            _commandEngine.Enqueue(command, token);

            await command.WaitAsync(token);
            return command.SearchResult;
        }

        internal SearchResult Search_Execute(SearchExCommandArgs args)
        {
            ////Thread.Sleep(3000); //##
            return new SearchResult(args.Keyword, args.Option, _core.Search(args.Keyword, args.Option));
        }



        internal void AddIndex(string root, string path)
        {
            var command = new NodeChangeCommand(this, new NodeChangeCommandArgs()
            {
                ChangeType = NodeChangeType.Add,
                Root = root,
                Path = path
            });
            _commandEngine.Enqueue(command);
        }


        internal void RemoveIndex(string root, string path)
        {
            var command = new NodeChangeCommand(this, new NodeChangeCommandArgs()
            {
                ChangeType = NodeChangeType.Remove,
                Root = root,
                Path = path
            });
            _commandEngine.Enqueue(command);
        }


        internal void RenameIndex(string root, string oldPath, string newPath)
        {
            var command = new NodeChangeCommand(this, new NodeChangeCommandArgs()
            {
                ChangeType = NodeChangeType.Rename,
                Root = root,
                OldPath = oldPath,
                Path = newPath
            });
            _commandEngine.Enqueue(command);
        }


        internal void RefleshIndex(string root, string path)
        {
            var command = new NodeChangeCommand(this, new NodeChangeCommandArgs()
            {
                ChangeType = NodeChangeType.Reflesh,
                Root = root,
                Path = path
            });
            _commandEngine.Enqueue(command);
        }

        internal void NodeChange_Execute(NodeChangeCommandArgs args)
        {
            switch (args.ChangeType)
            {
                case NodeChangeType.Add:
                    _core.AddPath(args.Root, args.Path);
                    break;
                case NodeChangeType.Remove:
                    _core.RemovePath(args.Root, args.Path);
                    break;
                case NodeChangeType.Rename:
                    _core.RenamePath(args.Root, args.OldPath, args.Path);
                    break;
                case NodeChangeType.Reflesh:
                    _core.RefleshIndex(args.Root, args.Path);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }


        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                    Stop();
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~SearchEngineEx() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }


    public class SearchedEventArgs : EventArgs
    {
        public SearchResult Result { get; set; }
    }

    
}
