// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;


namespace RealtimeSearch
{
    /// <summary>
    /// 
    /// </summary>
    public class NodeTree : IDisposable
    {
        public string Path { get; private set; }

        public Node Root { get; private set; }

        public bool IsDarty { get; private set; }


        // constructor
        public NodeTree(string path)
        {
            Path = path;
            IsDarty = true;

            InitializeWatcher();
        }

        //
        public void Collect()
        {
            if (!IsDarty)
            {
                Node.TotalCount += NodeCount();
                return;
            }
            IsDarty = false;

            // フォルダ監視開始
            _FileSystemWatcher.EnableRaisingEvents = true;

            // node
            Root = Node.CreateTree(Path, null, true);
            DumpTree();
        }

        //
        [Conditional("DEBUG")]
        public void DumpTree()
        {
            Debug.WriteLine("---- " + Path);
            Root.Dump();
        }

        // ノード数を返す
        public int NodeCount()
        {
            return Root.AllNodes.Count();
        }


        // 追加
        public Node AddNode(string path)
        {
            var node = Root.Add(path);
            Debug.WriteLine("Add: " + node?.Path);
            DumpTree();
            return node;
        }

        // 削除
        public Node RemoveNode(string path)
        {
            var node = Root.Remove(path);
            Debug.WriteLine("Del: " + node?.Path);
            DumpTree();
            return node;
        }

        // 名前変更
        public Node Rename(string oldPath, string newPath)
        {
            var node = Root.Search(oldPath);
            if (node != null)
            {
                // 場所の変更は認めない
                if (node.Parent?.Path != System.IO.Path.GetDirectoryName(newPath))
                {
                    throw new ApplicationException("リネームなのに場所が変更されている");
                }

                node.Rename(System.IO.Path.GetFileName(newPath));
                DumpTree();
            }

            return node;
        }

        // 更新
        public void RefleshNode(string path)
        {
            var node = Root.Search(path);
            if (node != null)
            {
                node.Reflesh();
            }
        }


        #region FileSystemWatcher 

        // ファイル変更監視
        private FileSystemWatcher _FileSystemWatcher;

        private void InitializeWatcher()
        {
            _FileSystemWatcher = new FileSystemWatcher();
            _FileSystemWatcher.Path = Path;
            _FileSystemWatcher.IncludeSubdirectories = true;
            _FileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size;
            _FileSystemWatcher.Created += Watcher_Created;
            _FileSystemWatcher.Deleted += Watcher_Deleted;
            _FileSystemWatcher.Renamed += Watcher_Renamed;
            _FileSystemWatcher.Changed += Watcher_Changed;
        }

        private void TerminateWatcher()
        {
            _FileSystemWatcher.Dispose();
            _FileSystemWatcher = null;
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            SearchEngine.Current.AddIndexRequest(Path, e.FullPath);
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            SearchEngine.Current.RemoveIndexRequest(Path, e.FullPath);
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            SearchEngine.Current.RenameIndexRequest(Path, e.OldFullPath, e.FullPath);
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            SearchEngine.Current.RefleshIndexRequest(Path, e.FullPath);
        }

        #endregion

        public void Dispose()
        {
            TerminateWatcher();
        }
    }
}
