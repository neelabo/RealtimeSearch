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

namespace NeeLaboratory.IO.Search.Files
{
    /// <summary>
    /// ファイル監視付きファイルツリー
    /// </summary>
    public class FileTree : NodeTree, IDisposable
    {
        private readonly string _path;
        private FileSystemWatcher? _fileSystemWatcher;
        private readonly SlimJobEngine _jobEngine;
        private readonly EnumerationOptions _enumerationOptions;
        private readonly bool _recurseSubdirectories;
        private readonly string _searchPattern;
        private bool _initialized;
        private bool _disposedValue;
        private int _count;
        private readonly SemaphoreSlim _semaphore = new(1, 1);


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
            path = LoosePath.TrimDirectoryEnd(path);
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException($"Directory not found: {path}");

            _path = path;

            _jobEngine = new SlimJobEngine(nameof(FileTree));

            _searchPattern = "*";
            _recurseSubdirectories = enumerationOptions.RecurseSubdirectories;
            _enumerationOptions = enumerationOptions.Clone();
            _enumerationOptions.RecurseSubdirectories = false;
        }


        public string Path => _path;

        /// <summary>
        /// おおよその総数。非同期に加算されるため不正確
        /// </summary>
        public int Count => _count;


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

            var sw = Stopwatch.StartNew();

            try
            {
                Trunk.ClearChildren();
                if (_recurseSubdirectories)
                {
                    CreateChildrenRecursive(Trunk, new DirectoryInfo(LoosePath.TrimDirectoryEnd(Trunk.FullName)), token);
                }
                else
                {
                    CreateChildrenTop(Trunk, new DirectoryInfo(LoosePath.TrimDirectoryEnd(Trunk.FullName)), token);
                }

                sw.Stop();
                Debug.WriteLine($"Initialize {_path}: {sw.ElapsedMilliseconds} ms, Count={Trunk.WalkChildren().Count()}");
                //Trunk.Dump();

                sw.Start();
                Validate();
                sw.Stop();
                Debug.WriteLine($"Validate {_path}: {sw.ElapsedMilliseconds} ms");

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
            UpdateContent(node, true);

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

        private void UpdateFile(string path, CancellationToken token)
        {
            if (_disposedValue) return;
            if (!_initialized) return;

            using var lockToken = _semaphore.Lock(token);
            UpdateFileCore(path, token);
        }

        private void UpdateFileCore(string path, CancellationToken token)
        { 
            var node = Find(path);
            if (node is null)
            {
                Trace($"Cannot Update: {path}");
                return;
            }

            UpdateContent(node, false);

            Trace($"Update: {path}");
        }

        protected virtual void AttachContent(Node? node, FileSystemInfo file)
        {
        }

        protected virtual void DetachContent(Node? node)
        {
        }

        protected virtual void UpdateContent(Node? node, bool isRecursive)
        {
        }

        protected FileSystemInfo CreateFileInfo(string path)
        {
            var attr = File.GetAttributes(path);
            var file = (FileSystemInfo)(attr.HasFlag(FileAttributes.Directory) ? new DirectoryInfo(path) : new FileInfo(path));
            return file;
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
            _jobEngine.InvokeAsync(() => UpdateFile(e.FullPath, CancellationToken.None));
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