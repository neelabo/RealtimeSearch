//#define LOCAL_DEBUG
using NeeLaboratory.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static NeeLaboratory.RealtimeSearch.FileItemTree;


namespace NeeLaboratory.RealtimeSearch
{
    public class FileItemForest : IFileItemTree, IDisposable
    {
        private readonly EnumerationOptions _enumerationOptions;
        private List<FileItemTree> _trees = new();
        private bool _disposedValue;


        public FileItemForest(EnumerationOptions enumerationOptions)
        {
            _enumerationOptions = enumerationOptions;
        }


        public event EventHandler<FileTreeContentChangedEventArgs>? AddContentChanged;
        public event EventHandler<FileTreeContentChangedEventArgs>? RemoveContentChanged;


        public void UpdateTrees(IEnumerable<string> paths )
        {
            var filedPaths = ValidatePathCollection(paths);

            var trees = filedPaths.Select(path => _trees.FirstOrDefault(e => e.Path == path) ?? CreateTree(path, _enumerationOptions)).ToList();
            var removes = _trees.Except(trees);

            _trees = trees;

            foreach(var tree in removes)
            {
                RemoveTree(tree);
            }

            // [DEV]
            Debug.WriteLine($"UpdateTrees:");
            foreach (var tree in trees)
            {
                Debug.WriteLine(tree.Path);
            }
        }

        private FileItemTree CreateTree(string path, EnumerationOptions enumerationOptions)
        {
            var tree = new FileItemTree(path, _enumerationOptions);
            tree.AddContentChanged += Tree_AddContentChanged;
            tree.RemoveContentChanged += Tree_RemoveContentChanged;
            return tree;
        }

        private void RemoveTree(FileItemTree tree)
        {
            tree.AddContentChanged -= Tree_AddContentChanged;
            tree.RemoveContentChanged -= Tree_RemoveContentChanged;
            tree.Dispose();
        }

        private void Tree_AddContentChanged(object? sender, FileTreeContentChangedEventArgs e)
        {
            AddContentChanged?.Invoke(this, e);
        }

        private void Tree_RemoveContentChanged(object? sender, FileTreeContentChangedEventArgs e)
        {
            RemoveContentChanged?.Invoke(this, e);
        }


        /// <summary>
        /// 親子関係のないパス
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        private List<string> ValidatePathCollection(IEnumerable<string> paths)
        {
            return paths.Where(e => !paths.Any(x => IsPathChild(e, x))).ToList();
        }

        /// <summary>
        /// pathA は pathB の子であるか？
        /// </summary>
        /// <param name="pathA"></param>
        /// <param name="pathB"></param>
        /// <returns></returns>
        private bool IsPathChild(string pathA, string pathB)
        {
            var a = FixDirectoryPath(pathA);
            var b = FixDirectoryPath(pathB);
            if (a.Length <= b.Length) return false;
            return a.StartsWith(b, StringComparison.OrdinalIgnoreCase);
        }

        private string FixDirectoryPath(string path)
        {
            return path.TrimEnd('\\') + '\\';
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (var tree in _trees)
                    {
                        tree.Dispose();
                    }
                    _trees.Clear();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task InitializeAsync(CancellationToken token)
        {
            var options = new ParallelOptions { MaxDegreeOfParallelism = 2 };
            await Parallel.ForEachAsync(_trees, options, async (tree, token) =>
            {
                await tree.InitializeAsync(token);
            });
        }

        public async Task<IDisposable> LockAsync(CancellationToken token)
        {
            var trees = _trees;
            var disposables = new DisposableCollection();
            foreach (var tree in trees)
            {
                disposables.Add(await tree.LockAsync(token));
            }
            return disposables;
        }

        public IEnumerable<FileItem> CollectFileItems()
        {
            var trees = _trees;
            foreach (var tree in trees)
            {
                foreach (var item in tree.CollectFileItems())
                {
                    yield return item;
                }
            }
        }

        public void RequestRename(string src, string dst)
        {
            var trees = _trees;
            foreach (var tree in trees)
            {
                if (tree.Find(src) != null)
                {
                    tree.RequestRename(src, dst);
                }
            }
        }
    }

}
