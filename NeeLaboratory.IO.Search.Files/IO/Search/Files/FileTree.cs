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
using NeeLaboratory.Linq;

namespace NeeLaboratory.IO.Search.Files
{
    /// <summary>
    /// ファイル監視付きファイルツリー
    /// </summary>
    public class FileTree : NodeTree<FileContent>, IDisposable
    {
        private readonly string _path;
        private FileSystemWatcher? _fileSystemWatcher;
        private readonly DelaySlimJobEngine _jobEngine;
        private readonly EnumerationOptions _enumerationOptions;
        private readonly bool _recurseSubdirectories;
        private readonly string _searchPattern;
        private bool _initialized;
        private bool _activated;
        private bool _disposedValue;
        private int _count;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private FileTreeMemento? _memento;
        private readonly FileArea _area;
        public int _collectBusyCount;


        public FileTree(FileArea area, FileTreeMemento? memento) : base(area.Path)
        {
            _area = area;

            _path = LoosePath.TrimDirectoryEnd(area.Path);
            if (!Directory.Exists(_path)) throw new DirectoryNotFoundException($"Directory not found: {_path}");

            _memento = memento;

            _jobEngine = new DelaySlimJobEngine(nameof(FileTree));

            _searchPattern = "*";

            var enumerationOptions = IOExtensions.CreateEnumerationOptions(area.IncludeSubdirectories, FileAttributes.None);
            _recurseSubdirectories = enumerationOptions.RecurseSubdirectories;
            _enumerationOptions = enumerationOptions.Clone();
            _enumerationOptions.RecurseSubdirectories = false;

            Trunk.Content = CreateFileContent(Trunk);
        }


        public event EventHandler<FileTreeContentChangedEventArgs>? AddContentChanged;
        public event EventHandler<FileTreeContentChangedEventArgs>? RemoveContentChanged;
        public event EventHandler<FileTreeContentChangedEventArgs>? ContentChanged;
        public event EventHandler<FileTreeCollectBusyChangedEventArgs>? CollectBusyChanged;


        /// <summary>
        /// ファイルの範囲
        /// </summary>
        public FileArea Area => _area;

        /// <summary>
        /// インデックス生成中表示用のカウント。非同期に加算されるためノード数としては不正確
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// インデックス作成中か
        /// </summary>
        public bool IsCollectBusy => _collectBusyCount > 0;


        #region IDisposable

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

        #endregion IDisposable

        #region Control

        /// <summary>
        /// <see cref="IsCollectBusy"/> 用 EnterScope
        /// </summary>
        /// <returns></returns>
        private IDisposable IsCollectBusy_EnterScope()
        {
            IncrementCollectBusyCount();
            return new AnonymousDisposable(() => DecrementCollectBusyCount());
        }

        private void IncrementCollectBusyCount()
        {
            var count = Interlocked.Increment(ref _collectBusyCount);
            CollectBusyChanged?.Invoke(this, new FileTreeCollectBusyChangedEventArgs(count > 0));
        }

        private void DecrementCollectBusyCount()
        {
            var count = Interlocked.Decrement(ref _collectBusyCount);
            CollectBusyChanged?.Invoke(this, new FileTreeCollectBusyChangedEventArgs(count > 0));
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

        /// <summary>
        /// 並列処理のためのアクセスロック (Async)
        /// </summary>
        public async Task<IDisposable> LockAsync(CancellationToken token)
        {
            return await _semaphore.LockAsync(token);
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

        #endregion Control

        #region Initialize

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


            try
            {
                _count = 0;

                if (_memento is null)
                {
                    InitializeFromFileSystem(token);
                }
                else
                {
                    InitializeFromCache(_memento, token);
                }

                Debug.Assert(Trunk.Content is not null);
                Debug.Assert(_activated);
                //Validate();
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
            Debug.WriteLine($"InitializeFromFileSystem {_path}: ...");
            var sw = Stopwatch.StartNew();

            using var section = IsCollectBusy_EnterScope();

            Trunk.ClearChildren();
            if (_recurseSubdirectories)
            {
                CreateChildrenRecursive(Trunk, new DirectoryInfo(LoosePath.TrimDirectoryEnd(Trunk.FullName)), token);
            }
            else
            {
                CreateChildrenTop(Trunk, new DirectoryInfo(LoosePath.TrimDirectoryEnd(Trunk.FullName)), token);
            }

            _activated = true;
            _initialized = true;
            Debug.WriteLine($"InitializeFromFileSystem {_path}: {sw.ElapsedMilliseconds} ms, Count={Trunk.WalkChildren().Count()}");
            //Trunk.Dump();
        }

        /// <summary>
        /// キャッシュから Tree を構築する
        /// </summary>
        /// <param name="memento"></param>
        /// <param name="token"></param>
        private void InitializeFromCache(FileTreeMemento memento, CancellationToken token)
        {
            var section = IsCollectBusy_EnterScope();

            if (!_activated)
            {
                Debug.WriteLine($"InitializeFromCache {_path}: ...");
                var sw = Stopwatch.StartNew();

                var trunk = FileTree.RestoreTree(memento);
                if (trunk is null) throw new InvalidOperationException();

                Debug.Assert(trunk.Content?.Path == Trunk.FullName);
                SetTrunk(trunk);
                _activated = true;

                Debug.WriteLine($"InitializeFromCache {_path}: {sw.ElapsedMilliseconds} ms, Count={Trunk.WalkChildren().Count()}");
            }

            // TODO: 非同期実行
            Task.Run(() =>
            {
                try
                {
                    var sw = Stopwatch.StartNew();
                    Debug.WriteLine($"InitializeFromCache {_path}: Update ...");
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(token, token);
                    UpdateNode(Trunk, cts.Token);
                    _memento = null;
                    _initialized = true;
                    Debug.WriteLine($"InitializeFromCache {_path}: Update done. {sw.ElapsedMilliseconds}ms");
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine($"InitializeFromCache {_path}: Update canceled.");
                }
                finally
                {
                    section.Dispose();
                }
            }, token);
        }

        private void UpdateNode(Node<FileContent> node, CancellationToken token)
        {
#if DEBUG
            // デバッグ用の遅延。キャッシュの更新が即時終了してしまうため。
            Thread.Sleep(10);
#endif

            Debug.Assert(node.Content is not null);
            var content = node.Content;
            if (content is null) return;

            // 処理子ノード数をカウント
            _count++;

            if (content.State == FileContentState.Stable && !content.IsDirectory) return;
            token.ThrowIfCancellationRequested();

            var info = CreateFileInfo(node);
            var unknownChildren = false;
            if (info.Exists)
            {
                var removes = new List<Node<FileContent>>();

                // ノードのコンテンツを更新
                // 最終更新日が同じであれば情報に変更なしとする
                if (info.LastWriteTime == content.LastWriteTime)
                {
                    unknownChildren = content.State == FileContentState.UnknownChildren;
                    content.State = FileContentState.Stable;
                }
                else
                {
                    Debug.WriteLine($"Node: Update: {node.FullName}");
                    unknownChildren = true;
                    content.State = FileContentState.StableReady;
                    _jobEngine.InvokeAsync(() => UpdateFile(node.FullName, info, token));
                }

                // ノードがディレクトリであるならば子ノードを更新
                if (content.IsDirectory)
                {
                    var children = node.CloneChildren();

                    var directory = (DirectoryInfo)info;

                    if (unknownChildren)
                    {
                        // 構成に変更があるときはエントリを再取得する
                        var map = children.ToDictionary(e => e.Name, e => e);
                        foreach (var entry in Directory.GetFileSystemEntries(content.Path).Select(e => System.IO.Path.GetFileName(e)))
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
                    }
                    else
                    {
                        // 構成に変更がない場合は個別の子ノードの更新
                        foreach (var childNode in children)
                        {
                            UpdateNode(childNode, token);
                        }
                    }

                    // 掃除。Unknown な子ノードを削除
                    removes.AddRange(children.Where(e => e.Content?.State == FileContentState.Unknown));
                }
                // ノードがファイルであるならば整合性のみチェック
                else
                {
                    // ファイルなのに子ノードがある場合は全部削除
                    if (node.Children is not null && node.Children.Count != 0)
                    {
                        removes.AddRange(node.CloneChildren());
                    }
                }

                // Unknown な子ノードを削除
                foreach (var entry in removes)
                {
                    Debug.WriteLine($"Node: Remove: {entry.FullName}");
                    _jobEngine.InvokeAsync(() => RemoveFile(entry.FullName, token));
                }
            }
            else
            {
                Debug.WriteLine($"Node: {node} not found.");
                _jobEngine.InvokeAsync(() => RemoveFile(node.FullName, token));
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
        public Node<FileContent> CreateChildrenTop(Node<FileContent> parent, DirectoryInfo directoryInfo, CancellationToken token)
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
        public Node<FileContent> CreateChildrenRecursive(Node<FileContent> parent, DirectoryInfo directoryInfo, CancellationToken token)
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
            var directoryNodes = new Node<FileContent>[directories.Count];
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
        private Node<FileContent> CreateNode(Node<FileContent> parent, FileSystemInfo info)
        {
            var node = new Node<FileContent>(info.Name) { Parent = parent };
            AttachContent(node, info);
            return node;
        }

        #endregion Initialize

        #region NodeActions

        private void AddFile(string path, CancellationToken token)
        {
            if (_disposedValue) return;
            if (!_activated) return;

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

            //Validate();
        }

        private void RenameFile(string path, string oldPath, CancellationToken token)
        {
            if (_disposedValue) return;
            if (!_activated) return;

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

            //Validate();
        }

        private void RemoveFile(string path, CancellationToken token)
        {
            if (_disposedValue) return;
            if (!_activated) return;

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

            //Validate();
        }

        private void UpdateFile(string path, FileSystemInfo? info, CancellationToken token)
        {
            Debug.Assert(info is null || info.FullName.TrimEnd('\\') == path);

            if (_disposedValue) return;
            if (!_activated) return;

            using var lockToken = _semaphore.Lock(token);
            UpdateFileCore(path, info, token);
        }

        private void UpdateFileCore(string path, FileSystemInfo? info, CancellationToken token)
        {
            Debug.Assert(info is null || info.FullName.TrimEnd('\\') == path);

            var node = Find(path);
            if (node is null)
            {
                Trace($"Cannot Update: {path}");
                return;
            }

            UpdateContent(node, info, false);

            Trace($"Update: {path}");
        }

        #endregion NodeActions

        #region ContentActions

        protected void AttachContent(Node<FileContent>? node, FileSystemInfo file)
        {
            if (node == null) return;

            Debug.Assert(node.FullName.TrimEnd() == file.FullName);
            Debug.Assert(node.Content is null);

            node.Content = new FileContent(file);
            //Trace($"Add: {node.Content}");
            AddContentChanged?.Invoke(this, new FileTreeContentChangedEventArgs(node.Content));
        }

        protected void DetachContent(Node<FileContent>? node)
        {
            if (node == null) return;

            var content = node.Content;
            node.Content = null;
            if (content is not null)
            {
                Trace($"Remove: {content}");
                RemoveContentChanged?.Invoke(this, new FileTreeContentChangedEventArgs(content));
            }
        }

        protected void UpdateContent(Node<FileContent>? node, FileSystemInfo? info, bool isRecursive)
        {
            if (node == null) return;
            Debug.Assert(info is null || info.FullName.TrimEnd('\\') == node.FullName);

            var content = node.Content;
            Debug.Assert(content is not null);
            if (content is null) return;

            content.SetFileInfo(info ?? CreateFileInfo(node));
            Trace($"Update: {content}");
            ContentChanged?.Invoke(this, new FileTreeContentChangedEventArgs(content));

            if (!isRecursive || node.Children is null) return;

            // 子ノード更新
            foreach (var n in node.Children)
            {
                UpdateContent(n, null, isRecursive);
            }
        }

        public IEnumerable<FileContent> CollectFileContents()
        {
            return Trunk.WalkChildren().Select(e => e.Content).WhereNotNull();
        }

        private static FileContent CreateFileContent(Node<FileContent> node)
        {
            return new FileContent(CreateFileInfo(node));
        }

        private static FileSystemInfo CreateFileInfo(Node<FileContent> node)
        {
            return CreateFileInfo(node.GetFullPath());
        }

        private static FileSystemInfo CreateFileInfo(string path)
        {
            var directory = new DirectoryInfo(path);
            if (directory.Exists) return directory;
            return new FileInfo(path);
        }

        #endregion ContentActions

        #region FileSystemWatcher

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
            _jobEngine.InvokeAsync(() => RemoveFile(e.FullPath, CancellationToken.None));
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

        #endregion FileSystemWatcher

        #region Memento
        // TODO: Memento が FileTree と等しく対応していないので、データ構造の見直しが必要

        public FileTreeMemento CreateTreeMemento()
        {
            // TODO: Trunk から生成してるけど、Root からでよくないか？
            return new FileTreeMemento(_area, CreateTreeMemento(Trunk));
        }

        public List<FileNodeMemento> CreateTreeMemento(Node<FileContent> node)
        {
            return node.WalkWithDepth().Select(e => CreateFileNode(e.Node, e.Depth)).ToList();
        }

        private FileNodeMemento CreateFileNode(Node<FileContent> node, int depth)
        {
            var content = node.Content;
            Debug.Assert(content is not null);
            if (content is null) return new FileNodeMemento(depth, node.Name);
            return new FileNodeMemento(depth, content.IsDirectory, node.Name, content.LastWriteTime, content.Size);
        }

        public static Node<FileContent>? RestoreTree(FileTreeMemento memento)
        {
            if (memento == null) return null;
            if (memento.FileArea is null) return null;
            if (memento.Nodes is null) return null;

            var directory = LoosePath.GetDirectoryName(memento.FileArea.Path) ?? "";

            Node<FileContent> root = new Node<FileContent>("");
            Node<FileContent> current = root;
            int depth = -1;
            foreach (var node in memento.Nodes)
            {
                memento.FileArea.Path.Split('\\', StringSplitOptions.RemoveEmptyEntries);
                Debug.Assert(node.Depth > 0 || memento.FileArea.GetName() == node.Name);

                while (node.Depth <= depth)
                {
                    current = current.Parent ?? throw new FormatException("Cannot get parent node.");
                    depth--;
                }
                if (node.Depth - 1 != depth) throw new FormatException("Node is missing.");

                var child = new Node<FileContent>(node.Name);
                current.AddChild(child);
                var path = System.IO.Path.Combine(directory, current.FullName, node.Name);
                child.Content = new FileContent(node.IsDirectory, path, node.Name, node.LastWriteTime, node.Size, FileContentState.Unknown);
                current = child;
                depth = node.Depth;
            }

            // Trunk
            Debug.Assert(root.Children?.Count == 1);
            var trunkNode = root.Children.First();
            root.RemoveChild(trunkNode);

            return trunkNode;
        }

        #endregion Memento

        #region Debug

        [Conditional("DEBUG")]
        public void DumpDepth()
        {
            foreach (var item in Trunk.WalkChildrenWithDepth())
            {
                var content = item.Node.Content;
                var fileNode = CreateFileNode(item.Node, item.Depth);
                Debug.WriteLine(fileNode);
            }
        }

        [Conditional("LOCAL_DEBUG")]
        private void Trace(string s, params object[] args)
        {
            Debug.WriteLine($"{nameof(FileTree)}: {string.Format(s, args)}");
        }

        #endregion Debug
    }
}