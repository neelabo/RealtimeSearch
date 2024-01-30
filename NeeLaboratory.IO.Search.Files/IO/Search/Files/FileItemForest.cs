﻿//#define LOCAL_DEBUG
using NeeLaboratory.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace NeeLaboratory.IO.Search.Files
{
    public class FileItemForest : IFileItemTree, IDisposable
    {
        private List<FileItemTree> _trees = new();
        private bool _disposedValue;


        public FileItemForest()
        {
        }


        public event EventHandler<FileTreeContentChangedEventArgs>? AddContentChanged;
        public event EventHandler<FileTreeContentChangedEventArgs>? RemoveContentChanged;
        public event EventHandler<FileTreeContentChangedEventArgs>? ContentChanged;


        public int Count => _trees.Sum(t => t.Count);


        public void SetSearchAreas(IEnumerable<FileArea> areas)
        {
            var oldies = _trees;

            var fixedAreas = ValidatePathCollection(areas);
            var trees = fixedAreas.Select(area => oldies.FirstOrDefault(e => e.Area == area) ?? CreateTree(area)).ToList();
            var removes = oldies.Except(trees);

            _trees = trees;

            foreach (var tree in removes)
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

        public void AddSearchAreas(IEnumerable<FileArea> areas)
        {
            var trees = _trees;
            SetSearchAreas(trees.Select(e => e.Area).Concat(areas));
        }


        private FileItemTree CreateTree(FileArea area)
        {
            var tree = new FileItemTree(area);
            tree.AddContentChanged += Tree_AddContentChanged;
            tree.RemoveContentChanged += Tree_RemoveContentChanged;
            tree.ContentChanged += Tree_ContentChanged;
            return tree;
        }

        private void RemoveTree(FileItemTree tree)
        {
            tree.AddContentChanged -= Tree_AddContentChanged;
            tree.RemoveContentChanged -= Tree_RemoveContentChanged;
            tree.ContentChanged -= Tree_ContentChanged;
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

        private void Tree_ContentChanged(object? sender, FileTreeContentChangedEventArgs e)
        {
            ContentChanged?.Invoke(this, e);
        }

        /// <summary>
        /// 親子関係のないパス
        /// </summary>
        /// <param name="areas"></param>
        /// <returns></returns>
        private List<FileArea> ValidatePathCollection(IEnumerable<FileArea> areas)
        {
            return areas.Where(e => !areas.Any(x => x.Contains(e))).ToList();
        }


#if false
        /// <summary>
        /// pathA は pathB の子であるか？
        /// </summary>
        /// <param name="pathA"></param>
        /// <param name="pathB"></param>
        /// <returns></returns>
        private bool IsPathChild(NodeArea pathA, NodeArea pathB)
        {
            if (pathA == pathB) return false;

            var a = FixDirectoryPath(pathA);
            var b = FixDirectoryPath(pathB);
            if (a.Length <= b.Length) return false;
            return a.StartsWith(b, StringComparison.OrdinalIgnoreCase);
        }

        private string FixDirectoryPath(string path)
        {
            return path.TrimEnd('\\') + '\\';
        }
#endif

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

        public void Initialize(CancellationToken token)
        {
            var options = new ParallelOptions { CancellationToken = token };
            Parallel.ForEach(_trees, options, tree =>
            {
                tree.Initialize(token);
            });
        }

        public async Task InitializeAsync(CancellationToken token)
        {
            var options = new ParallelOptions { CancellationToken = token };
            await Parallel.ForEachAsync(_trees, options, async (tree, token) =>
            {
                await tree.InitializeAsync(token);
            });
        }

        public IDisposable Lock(CancellationToken token)
        {
            var trees = _trees;
            var disposables = new DisposableCollection();
            foreach (var tree in trees)
            {
                disposables.Add(tree.Lock(token));
            }
            return disposables;
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

        public void Wait(CancellationToken token)
        {
            var trees = _trees;
            foreach (var tree in trees)
            {
                tree.Wait(token);
            }
        }

        public async Task WaitAsync(CancellationToken token)
        {
            var options = new ParallelOptions { CancellationToken = token };
            await Parallel.ForEachAsync(_trees, options, async (tree, token) =>
            {
                await tree.WaitAsync(token);
            });
        }

    }

}