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
using System.Security.Cryptography.X509Certificates;


namespace NeeLaboratory.IO.Nodes
{
    /// <summary>
    /// ファイル監視付きファイルツリー
    /// </summary>
    public class FileTree : NodeTree, IDisposable
    {
        private readonly string _path;
        private FileSystemWatcher? _fileSystemWatcher;
        private readonly SingleJobEngine _jobEngine;
        private readonly EnumerationOptions _enumerationOptions;
        private readonly bool _recurseSubdirectories;
        private readonly string _searchPattern;
        private bool _initialized;
        private bool _disposedValue;
        private readonly AsyncLock _asyncLock = new();


        /// <summary>
        /// コンストラクタ。
        /// 使用するには InitializeAsync() でデータを初期化する必要があります。
        /// </summary>
        /// <param name="path">検索パス</param>
        /// <param name="enumerationOptions">検索オプション</param>
        /// <exception cref="ArgumentException">絶対パスでない</exception>
        /// <exception cref="DirectoryNotFoundException">ディレクトリが見つからない</exception>
        public FileTree(string path, EnumerationOptions enumerationOptions) : base(path)
        {
            if (string.Compare(path, System.IO.Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase) != 0) throw new ArgumentException($"Not an absolute path: {path}");
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException($"Directory not found: {path}");

            _path = path;

            _jobEngine = new SingleJobEngine(nameof(FileTree));
            _jobEngine.StartEngine();

            _searchPattern = "*";
            _recurseSubdirectories = enumerationOptions.RecurseSubdirectories;
            _enumerationOptions = enumerationOptions.Clone();
            _enumerationOptions.RecurseSubdirectories = false;
        }


        public string Path => _path;


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

        /// <summary>
        /// 並列処理のためのアクセスロック
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<IDisposable> LockAsync(CancellationToken token)
        {
            return await _asyncLock.LockAsync(token);
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task InitializeAsync(CancellationToken token)
        {
            if (_disposedValue) return;

            // NOTE: JobEngine の寿命とリンク
            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, _jobEngine.CancellationToken);
            var linkedToken = linkedTokenSource.Token;

            using (await _asyncLock.LockAsync(linkedToken))
            {
                if (_initialized) return;
                InitializeWatcher(_recurseSubdirectories);

                Trace($"Initialize: ...");

                await Task.Run(async () =>
                {
                    var sw = Stopwatch.StartNew();

                    try
                    {
                        Trunk.ClearChildren();
                        if (_recurseSubdirectories)
                        {
                            CreateChildrenRecursive(Trunk, new DirectoryInfo(Trunk.FullName), token);
                        }
                        else
                        {
                            CreateChildrenTop(Trunk, new DirectoryInfo(Trunk.FullName), token);
                        }

                        sw.Stop();
                        Debug.WriteLine($"Initialize: {sw.ElapsedMilliseconds} ms, Count={Trunk.WalkChildren().Count()}");
                        //Trunk.Dump();

                        Validate();
                        _initialized = true;
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine($"Initialize: Canceled.");
                    }
                    catch (AggregateException ae)
                    {
                        var ignoreExceptions = ae.Flatten().InnerExceptions.Where(ex => ex is not OperationCanceledException);
                        if (ignoreExceptions.Any())
                        {
                            throw new AggregateException(ignoreExceptions);
                        }
                    }

                    await Task.CompletedTask;
                });
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

            var entries = directoryInfo.GetFileSystemInfos(_searchPattern, _enumerationOptions);
            token.ThrowIfCancellationRequested();

            // パラレルにしたほうが速いね
            var directories = entries.OfType<DirectoryInfo>().ToList();
            var directoryNodes = new Node[directories.Count];

            var parallelOptions = new ParallelOptions() { CancellationToken = token };
            Parallel.ForEach(directories, parallelOptions, (s, state, index) =>
            {
                directoryNodes[(int)index] = CreateChildrenRecursive(CreateNode(parent, s), s, token);
            });

            var fileNodes = entries.OfType<FileInfo>().Select(s => CreateNode(parent, s));
            parent.Children = directoryNodes.Concat(fileNodes).ToList();
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

        /// <summary>
        /// 構造変更処理
        /// </summary>
        /// <param name="action"></param>
        /// <param name="e"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task FileSystemActionAsync(FileSystemAction action, FileSystemEventArgs e, CancellationToken token)
        {
            using (await _asyncLock.LockAsync(token))
            {
                if (!_initialized) throw new InvalidOperationException("Not initialized");
                try
                {
                    switch (action)
                    {
                        case FileSystemAction.Created:
                            Add(e.FullPath, token);
                            break;
                        case FileSystemAction.Renamed:
                            Rename(e.FullPath, ((RenamedEventArgs)e).OldFullPath, token);
                            break;
                        case FileSystemAction.Deleted:
                            Remove(e.FullPath, token);
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"FileTree.FileSystemActionAsync: {action}, {e.FullPath}");
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        private void Add(string path, CancellationToken token)
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

        private void Rename(string path, string oldPath, CancellationToken token)
        {
            var node = Find(oldPath);
            if (node is null)
            {
                Trace($"Cannot Rename: NofFound: {path}");
                return;
            }

            // 名前だけ変更以外は受け付けない
            if (System.IO.Path.GetDirectoryName(path) != System.IO.Path.GetDirectoryName(oldPath))
            {
                Debug.WriteLine($"Cannot Rename: Other than the name: {path}");
                return;
            }

            Trace($"Rename: {oldPath} -> {path}");
            Rename(oldPath, System.IO.Path.GetFileName(path));

            // コンテンツ更新
            OnUpdateContent(node, true);

            Validate();
        }

        private void Remove(string path, CancellationToken token)
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

        protected virtual void AttachContent(Node? node, FileSystemInfo file)
        {
        }

        protected virtual void DetachContent(Node? node)
        {
        }

        protected virtual void OnUpdateContent(Node? node, bool isRecursive)
        {
        }

        protected FileSystemInfo CreateFileInfo(string path)
        {
            var attr = File.GetAttributes(path);
            var file = (FileSystemInfo)(attr.HasFlag(FileAttributes.Directory) ? new DirectoryInfo(path) : new System.IO.FileInfo(path));
            return file;
        }

        /// <summary>
        /// ファイル監視初期化
        /// </summary>
        private void InitializeWatcher(bool includeSubdirectories)
        {
            try
            {
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
            _jobEngine.Enqueue(new FileSystemJob(this, FileSystemAction.Created, e));
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Trace($"Watcher deleted: {e.FullPath}");
            _jobEngine.Enqueue(new FileSystemJob(this, FileSystemAction.Deleted, e));
        }

        private void Watcher_Renamed(object? sender, RenamedEventArgs e)
        {
            Trace($"Watcher renamed: {e.OldFullPath} => {e.Name}");
            _jobEngine.Enqueue(new FileSystemJob(this, FileSystemAction.Renamed, e));
        }

        private void Watcher_Changed(object? sender, FileSystemEventArgs e)
        {
            // 情報更新？
        }

        // 名前変更要求
        public void RequestRename(string src, string dst)
        {
            var directory = System.IO.Path.GetDirectoryName(src) ?? "";
            if (directory != System.IO.Path.GetDirectoryName(dst)) throw new ArgumentException("The directories are different.");
            
            var args = new RenamedEventArgs(WatcherChangeTypes.Renamed, directory, System.IO.Path.GetFileName(dst), System.IO.Path.GetFileName(src));
            _jobEngine.Enqueue(new FileSystemJob(this, FileSystemAction.Renamed, args));
        }

        [Conditional("LOCAL_DEBUG")]
        private void Trace(string s, params object[] args)
        {
            Debug.WriteLine($"{nameof(FileTree)}: {string.Format(s, args)}");
        }


        public class FileSystemJob : JobBase
        {
            private readonly FileTree _tree;
            private readonly FileSystemAction _action;
            private readonly FileSystemEventArgs _eventArgs;

            public FileSystemJob(FileTree tree, FileSystemAction action, FileSystemEventArgs eventArgs)
            {
                _tree = tree;
                _action = action;
                _eventArgs = eventArgs;
            }

            protected override async Task ExecuteAsync(CancellationToken token)
            {
                await _tree.FileSystemActionAsync(_action, _eventArgs, token);
            }
        }

        public enum FileSystemAction
        {
            Created,
            Deleted,
            Renamed,
        };
    }
}