//#define LOCAL_DEBUG

using NeeLaboratory.Threading;
using NeeLaboratory.Threading.Jobs;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using NeeLaboratory.Collections;
using NeeLaboratory.ComponentModel;

namespace NeeLaboratory.IO.Search.Files
{
    /// <summary>
    /// ファイル監視付きファイルツリー
    /// </summary>
    public class FileTree : NodeTree, IDisposable
    {
        private readonly string _path;
        private FileSystemWatcher? _fileSystemWatcher;
        private readonly DelaySlimJobEngine _jobEngine;
        private readonly EnumerationOptions _enumerationOptions;
        private readonly bool _recurseSubdirectories;
        private readonly string _searchPattern;
        private bool _initialized;
        private bool _disposedValue;
        private int _count;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private FileTreeMemento? _memento;
        private bool _isCollectBusy;


        /// <summary>
        /// コンストラクタ。
        /// 使用するには InitializeAsync() でデータを初期化する必要があります。
        /// </summary>
        /// <param name="path">検索パス</param>
        /// <param name="enumerationOptions">検索オプション</param>
        /// <exception cref="ArgumentException">絶対パスでない</exception>
        /// <exception cref="DirectoryNotFoundException">ディレクトリが見つからない</exception>
        public FileTree(string path, FileTreeMemento? memento, EnumerationOptions enumerationOptions) : base(path)
        {
            path = LoosePath.TrimDirectoryEnd(path);
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException($"Directory not found: {path}");

            _path = path;
            _memento = memento;

            _jobEngine = new DelaySlimJobEngine(nameof(FileTree));

            _searchPattern = "*";
            _recurseSubdirectories = enumerationOptions.RecurseSubdirectories;
            _enumerationOptions = enumerationOptions.Clone();
            _enumerationOptions.RecurseSubdirectories = false;
        }


        public event EventHandler<bool>? CollectBusyChanged;


        public string Path => _path;

        /// <summary>
        /// おおよその総数。非同期に加算されるため不正確
        /// </summary>
        public int Count => _count;

        public bool IsCollectBusy
        {
            get { return _isCollectBusy; }
            set
            {
                if (_isCollectBusy != value)
                {
                    _isCollectBusy = value;
                    CollectBusyChanged?.Invoke(this, _isCollectBusy);
                }
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                }

                TerminateWatcher();
                _jobEngine.Dispose();

                _disposedValue = true;
            }
        }

        ~FileTree()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private IDisposable CreateCollectSection()
        {
            IsCollectBusy = true;
            return new AnonymousDisposable(() => IsCollectBusy = false);
        }

        /// <summary>
        /// 並列処理のためのアクセスロック
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public IDisposable Lock(CancellationToken token)
        {
            return _semaphore.Lock(token);
        }

        public async Task<IDisposable> LockAsync(CancellationToken token)
        {
            return await _semaphore.LockAsync(token);
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task InitializeAsync(CancellationToken token)
        {
            if (_disposedValue) return;

            await _jobEngine.InvokeAsync(() => InitializeInner(token));
        }

        public void Initialize(CancellationToken token)
        {
            if (_disposedValue) return;
            _jobEngine.Invoke(() => InitializeInner(token));
        }

        private void InitializeInner(CancellationToken token)
        {
            if (_disposedValue) return;

            if (_initialized) return;

            using var lockToken = _semaphore.Lock(token);

            InitializeWatcher(_recurseSubdirectories);

            Trace($"Initialize {_path}: ...");

            try
            {
                if (_memento is null)
                {
                    InitializeFromFileSystem(token);
                }
                else
                {
                    InitializeFromCache(_memento, token);
                    _memento = null;
                }

                Debug.Assert(Trunk.Content is FileItem);
                //Validate();

                _initialized = true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"Initialize {_path}: Canceled.");
            }
            catch (AggregateException ae)
            {
                var ignoreExceptions = ae.Flatten().InnerExceptions.Where(ex => ex is not OperationCanceledException);
                if (ignoreExceptions.Any())
                {
                    throw new AggregateException(ignoreExceptions);
                }
            }
        }

        /// <summary>
        /// ファイルシステムから Tree を構築する
        /// </summary>
        /// <param name="token"></param>
        /// <exception cref="AggregateException"></exception>
        private void InitializeFromFileSystem(CancellationToken token)
        {
            var sw = Stopwatch.StartNew();

            using var section = CreateCollectSection();

            Trunk.ClearChildren();
            if (_recurseSubdirectories)
            {
                CreateChildrenRecursive(Trunk, new DirectoryInfo(LoosePath.TrimDirectoryEnd(Trunk.FullName)), token);
            }
            else
            {
                CreateChildrenTop(Trunk, new DirectoryInfo(LoosePath.TrimDirectoryEnd(Trunk.FullName)), token);
            }

            Debug.WriteLine($"Initialize {_path}: {sw.ElapsedMilliseconds} ms, Count={Trunk.WalkChildren().Count()}");
            //Trunk.Dump();
        }

        /// <summary>
        /// キャッシュから Tree を構築する
        /// </summary>
        /// <param name="memento"></param>
        /// <param name="token"></param>
        private void InitializeFromCache(FileTreeMemento memento, CancellationToken token)
        {
            var sw = Stopwatch.StartNew();

            var section = CreateCollectSection();

            var trunk = FileItemTree.RestoreTree(memento);
            if (trunk is null) throw new InvalidOperationException();

            Debug.Assert((trunk.Content as FileItem)?.Path == Trunk.FullName);
            SetTrunk(trunk);

            Debug.WriteLine($"Initialize {_path}: FromCache: {sw.ElapsedMilliseconds} ms, Count={Trunk.WalkChildren().Count()}");

            // TODO: 非同期実行
            Task.Run(async () =>
            {
                try
                {
#if DEBUG
                    await Task.Delay(3000);
#endif
                    var sw = Stopwatch.StartNew();
                    UpdateNode(Trunk, token);
                    Debug.WriteLine($"Initialize {_path}: Update done. {sw.ElapsedMilliseconds}ms");
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    section.Dispose();
                }
            }, token);
        }

        private void UpdateNode(Node node, CancellationToken token)
        {
            Debug.Assert(node.Content is FileItem);
            if (node.Content is not FileItem fileItem) return;

            if (fileItem.State == FileItemState.Stable) return;
            token.ThrowIfCancellationRequested();

            var info = CreateFileInfo(fileItem.Path);
            var isUpdate = false;
            if (info.Exists)
            {
                var removes = new List<Node>();

                if (info.LastWriteTime == fileItem.LastWriteTime) // 最終更新日だけチェック
                {
                    fileItem.State = FileItemState.Stable;
                }
                else
                { 
                    Debug.WriteLine($"Node: Update: {node.FullName}");
                    fileItem.State = FileItemState.Known;
                    _jobEngine.InvokeAsync(() => UpdateFile(node.FullName, info, token));
                    isUpdate = true;
                }

                if (fileItem.IsDirectory)
                {
                    lock (node.ChildLock)
                    {
                        var directory = (DirectoryInfo)info;
                        if (isUpdate)
                        {
                            // 構成に変更があるときはエントリを再取得する
                            var map = node.ChildCollection().ToDictionary(e => e.Name, e => e);
                            foreach (var entry in Directory.GetFileSystemEntries(fileItem.Path).Select(e => System.IO.Path.GetFileName(e)))
                            {
                                if (map.TryGetValue(entry, out var childNode))
                                {
                                    // 存在するものは通常の更新
                                    UpdateNode(childNode, token);
                                }
                                else
                                {
                                    // 存在しないものは新しく追加
                                    var path = System.IO.Path.Combine(node.FullName, entry);
                                    Debug.WriteLine($"Node: Add: {path}");
                                    _jobEngine.InvokeAsync(() => AddFile(path, token));
                                }
                            }

                            // 掃除
                            removes.AddRange(node.ChildCollection().Where(e => (e.Content as FileItem)?.State == FileItemState.Unknown));
                        }
                        else
                        {
                            // 構成に変更がない場合は個別の子ノードの更新
                            foreach (var childNode in node.ChildCollection())
                            {
                                UpdateNode(childNode, token);
                                if ((childNode.Content as FileItem)?.State == FileItemState.Unknown)
                                {
                                    removes.Add(childNode);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // ファイルなのに子ノードがある場合は全部削除
                    if (node.Children is not null && node.Children.Count != 0)
                    {
                        lock (node.ChildLock) // いらないはずだが一応
                        {
                            removes.AddRange(node.Children);
                        }
                    }
                }

                // Unknown な子ノードを削除
                foreach (var entry in removes)
                {
                    Debug.WriteLine($"Node: Remove: {entry.FullName}");
                    _jobEngine.InvokeAsync(() => RemoveFile(entry.FullName, token));
                }

                Interlocked.Add(ref _count, node.Children?.Count ?? 0);
            }
            else
            {
                Debug.WriteLine($"Node: {node} not found.");
            }
        }

        /// <summary>
        /// 子ノード生成 再帰なし
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="directoryInfo"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Node CreateChildrenTop(Node parent, DirectoryInfo directoryInfo, CancellationToken token)
        {
            Debug.Assert(_enumerationOptions.RecurseSubdirectories == false);
            if (directoryInfo is null) throw new ArgumentNullException(nameof(directoryInfo));
            if (!directoryInfo.Exists) throw new DirectoryNotFoundException($"Directory not found: {nameof(directoryInfo)}");

            // 既に子が定義されているなら処理しない
            if (parent.Children is not null)
            {
                Trace($"CreateChildrenRecursive: Children already exists: {directoryInfo.FullName}");
                return parent;
            }

            var entries = directoryInfo.GetFileSystemInfos(_searchPattern, _enumerationOptions);
            token.ThrowIfCancellationRequested();

            parent.Children = entries.Select(s => CreateNode(parent, s)).ToList();
            return parent;
        }

        /// <summary>
        /// 子ノード生成 再帰あり
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="directoryInfo"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Node CreateChildrenRecursive(Node parent, DirectoryInfo directoryInfo, CancellationToken token)
        {
            Debug.Assert(_enumerationOptions.RecurseSubdirectories == false);
            if (directoryInfo is null) throw new ArgumentNullException(nameof(directoryInfo));
            if (!directoryInfo.Exists) throw new DirectoryNotFoundException($"Directory not found: {nameof(directoryInfo)}");

            // 既に子が定義されているなら処理しない
            if (parent.Children is not null)
            {
                Trace($"CreateChildrenRecursive: Children already exists: {directoryInfo.FullName}");
                return parent;
            }

            FileSystemInfo[] entries;
            try
            {
                entries = directoryInfo.GetFileSystemInfos(_searchPattern, _enumerationOptions);
            }
            catch (Exception ex)
            {
                // TODO: ログに出力
                Debug.WriteLine($"IO Error: {ex.Message}");
                entries = [];
            }

            token.ThrowIfCancellationRequested();

            // パラレルにしたほうが速いね
#if true
            var directories = entries.OfType<DirectoryInfo>().ToList();
            var directoryNodes = new Node[directories.Count];
            var parallelOptions = new ParallelOptions() { CancellationToken = token };
            Parallel.ForEach(directories, parallelOptions, (s, state, index) =>
            {
                directoryNodes[(int)index] = CreateChildrenRecursive(CreateNode(parent, s), s, token);
            });
#else
            var directoryNodes = entries.OfType<DirectoryInfo>()
                .Select(e => CreateChildrenRecursive(CreateNode(parent, e), e, token))
                .ToList();
#endif

            var fileNodes = entries.OfType<FileInfo>().Select(s => CreateNode(parent, s));
            parent.Children = directoryNodes.Concat(fileNodes).ToList();
            Interlocked.Add(ref _count, parent.Children.Count);
            return parent;
        }

        /// <summary>
        /// ノード生成
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        private Node CreateNode(Node parent, FileSystemInfo info)
        {
            var node = new Node(info.Name) { Parent = parent };
            AttachContent(node, info);
            return node;
        }

        private void AddFile(string path, CancellationToken token)
        {
            if (_disposedValue) return;
            if (!_initialized) return;

            using var lockToken = _semaphore.Lock(token);
            AddFileCore(path, token);
        }

        private void AddFileCore(string path, CancellationToken token)
        {
            var info = CreateFileInfo(path);
            if ((info.Attributes & _enumerationOptions.AttributesToSkip) != 0)
            {
                Trace($"Cannot Add: AttributeToSkip: {path}");
                return;
            }

            var node = Add(info.FullName);
            if (node is null)
            {
                Trace($"Cannot Add: {path}");
                return;
            }

            Trace($"Add: {path}");
            Debug.Assert(node.FullName == path);
            AttachContent(node, info);

            if (_recurseSubdirectories && info is DirectoryInfo directoryInfo)
            {
                CreateChildrenRecursive(node, directoryInfo, token);
            }

            Validate();
        }

        private void RenameFile(string path, string oldPath, CancellationToken token)
        {
            if (_disposedValue) return;
            if (!_initialized) return;

            using var lockToken = _semaphore.Lock(token);
            RenameFileCore(path, oldPath, token);
        }

        private void RenameFileCore(string path, string oldPath, CancellationToken token)
        {
            // 名前だけ変更以外は受け付けない
            if (System.IO.Path.GetDirectoryName(path) != System.IO.Path.GetDirectoryName(oldPath))
            {
                Debug.Assert(false, $"Cannot Rename: Other than the name: {path}");
                return;
            }

            var node = Find(oldPath);
            if (node is null)
            {
                // NOTE: 大文字、小文字の名前変更のときは Changed イベント前に Deleted イベントが発行されるので変更元は見つからない
                Trace($"Cannot Rename: NofFound: {path}");
                AddFileCore(path, token);
                return;
            }

            Trace($"Rename: {oldPath} -> {path}");
            Rename(oldPath, System.IO.Path.GetFileName(path));

            // コンテンツ更新
            UpdateContent(node, null, true);

            Validate();
        }

        private void RemoveFile(string path, CancellationToken token)
        {
            if (_disposedValue) return;
            if (!_initialized) return;

            using var lockToken = _semaphore.Lock(token);
            RemoveFileCore(path, token);
        }

        private void RemoveFileCore(string path, CancellationToken token)
        {
            var node = Remove(path);
            if (node is null)
            {
                Trace($"Cannot Removed: {path}");
                return;
            }

            Trace($"Removed: {path}");

            // 自身と子のコンテンツクリア
            foreach (var n in node.Walk())
            {
                DetachContent(n);
            }

            Validate();
        }

        private void UpdateFile(string path, FileSystemInfo? info, CancellationToken token)
        {
            Debug.Assert(info is null || info.FullName == path);

            if (_disposedValue) return;
            if (!_initialized) return;

            using var lockToken = _semaphore.Lock(token);
            UpdateFileCore(path, info, token);
        }

        private void UpdateFileCore(string path, FileSystemInfo? info, CancellationToken token)
        {
            Debug.Assert(info is null || info.FullName == path);

            var node = Find(path);
            if (node is null)
            {
                Trace($"Cannot Update: {path}");
                return;
            }

            UpdateContent(node, info, false);

            Trace($"Update: {path}");
        }

        protected virtual void AttachContent(Node? node, FileSystemInfo file)
        {
        }

        protected virtual void DetachContent(Node? node)
        {
        }

        protected virtual void UpdateContent(Node? node, FileSystemInfo? info, bool isRecursive)
        {
        }

        protected static FileSystemInfo CreateFileInfo(string path)
        {
            var file = new FileInfo(path);
            if (file.Exists) return file;
            var directory = new DirectoryInfo(path);
            //Debug.Assert(directory.Exists);
            return directory;
          
#if false
            var attr = File.GetAttributes(path);
            var file = (FileSystemInfo)(attr.HasFlag(FileAttributes.Directory) ? new DirectoryInfo(path) : new FileInfo(path));
            return file;
#endif
        }

        /// <summary>
        /// ファイル監視初期化
        /// </summary>
        private void InitializeWatcher(bool includeSubdirectories)
        {
            try
            {
                TerminateWatcher();
                _fileSystemWatcher = new FileSystemWatcher
                {
                    Path = _path,
                    IncludeSubdirectories = includeSubdirectories,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.LastWrite
                };
                _fileSystemWatcher.Created += Watcher_Created;
                _fileSystemWatcher.Deleted += Watcher_Deleted;
                _fileSystemWatcher.Renamed += Watcher_Renamed;
                _fileSystemWatcher.Changed += Watcher_Changed;

                _fileSystemWatcher.EnableRaisingEvents = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                TerminateWatcher();
            }
        }

        /// <summary>
        /// ファイル監視終了処理
        /// </summary>
        private void TerminateWatcher()
        {
            if (_fileSystemWatcher != null)
            {
                _fileSystemWatcher.EnableRaisingEvents = false;
                _fileSystemWatcher.Dispose();
            }
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            Trace($"Watcher created: {e.FullPath}");
            _jobEngine.InvokeAsync(() => AddFile(e.FullPath, CancellationToken.None));
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Trace($"Watcher deleted: {e.FullPath}");
            // 大文字・小文字のみの Rename では先に Deleted が発生するため処理を遅延させる
            _jobEngine.InvokeDelayAsync(() => RemoveFile(e.FullPath, CancellationToken.None), 100);
        }

        private void Watcher_Renamed(object? sender, RenamedEventArgs e)
        {
            Trace($"Watcher renamed: {e.OldFullPath} => {e.Name}");
            _jobEngine.InvokeAsync(() => RenameFile(e.FullPath, e.OldFullPath, CancellationToken.None));
        }

        private void Watcher_Changed(object? sender, FileSystemEventArgs e)
        {
            Trace($"Watcher changed: {e.FullPath}");
            _jobEngine.InvokeAsync(() => UpdateFile(e.FullPath, null, CancellationToken.None));
        }

        /// <summary>
        /// 全てのコマンドの完了待機
        /// </summary>
        public void Wait(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_disposedValue) throw new ObjectDisposedException(GetType().FullName);

            _jobEngine.InvokeAsync(() => { }, token).Wait(token);
        }

        /// <summary>
        /// 全てのコマンドの完了待機
        /// </summary>
        public async Task WaitAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (_disposedValue) throw new ObjectDisposedException(GetType().FullName);

            await _jobEngine.InvokeAsync(() => { }, token);
        }


        [Conditional("LOCAL_DEBUG")]
        private void Trace(string s, params object[] args)
        {
            Debug.WriteLine($"{nameof(FileTree)}: {string.Format(s, args)}");
        }
    }
}