//#define LOCAL_DEBUG

using NeeLaboratory.IO;
using NeeLaboratory.IO.Nodes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NodeArea = NeeLaboratory.IO.Search.FileNode.NodeArea;


namespace NeeLaboratory.RealtimeSearch
{
    public class FileItemTree : FileTree, IFileItemTree
    {
        private NodeArea _area;

        public FileItemTree(NodeArea area) : base(area.Path, CreateEnumerationOptions(area))
        {
            _area = area;
        }


        public event EventHandler<FileTreeContentChangedEventArgs>? AddContentChanged;
        public event EventHandler<FileTreeContentChangedEventArgs>? RemoveContentChanged;
        public event EventHandler<FileTreeContentChangedEventArgs>? ContentChanged;


        public class FileTreeContentChangedEventArgs : EventArgs
        {
            public FileTreeContentChangedEventArgs(FileItem fileItem)
                : this(fileItem, null)
            {
            }

            public FileTreeContentChangedEventArgs(FileItem fileItem, FileItem? oldFileItem)
            {
                FileItem = fileItem;
                OldFileItem = oldFileItem;
            }

            public FileItem FileItem { get; }
            public FileItem? OldFileItem { get; }
        }


        public NodeArea Area => _area;


        private static EnumerationOptions CreateEnumerationOptions(NodeArea area)
        {
            var options = IOExtensions.CreateEnumerationOptions(area.IncludeSubdirectories, FileAttributes.None);
            return options;
        }

        protected override void AttachContent(Node? node, FileSystemInfo file)
        {
            if (node == null) return;

            Debug.Assert(node.FullName == file.FullName);

            var fileItem = CreateFileItem(node);
            Trace($"Add: {fileItem}");
            AddContentChanged?.Invoke(this, new FileTreeContentChangedEventArgs(fileItem));
        }

        protected override void DetachContent(Node? node)
        {
            if (node == null) return;

            var fileItem = node.Content as FileItem;
            node.Content = null;
            if (fileItem is not null)
            {
                Trace($"Remove: {fileItem}");
                RemoveContentChanged?.Invoke(this, new FileTreeContentChangedEventArgs(fileItem));
            }
        }

        protected override void UpdateContent(Node? node, bool isRecursive)
        {
            if (node == null) return;

            var fileItem = GetFileItem(node);
            fileItem.SetFileInfo(CreateFileInfo(node.FullName));
            Trace($"Update: {fileItem}");
            ContentChanged?.Invoke(this, new FileTreeContentChangedEventArgs(fileItem));

            if (!isRecursive || node.Children is null) return;

            // 子ノード更新
            foreach (var n in node.Children)
            {
                UpdateContent(n, isRecursive);
            }
        }

        public IEnumerable<FileItem> CollectFileItems()
        {
            return Trunk.WalkChildren().Select(e => GetFileItem(e));
        }

        private FileItem GetFileItem(Node node)
        {
            if (node.Content is FileItem fileItem)
            {
                return fileItem;
            }

            return CreateFileItem(node);
        }

        private FileItem CreateFileItem(Node node)
        {
            var info = CreateFileInfo(node.FullName);
            var fileItem = new FileItem(info);
            node.Content = fileItem;
            return fileItem;
        }


        [Conditional("LOCAL_DEBUG")]
        private void Trace(string s, params object[] args)
        {
            Debug.WriteLine($"{this.GetType().Name}: {string.Format(s, args)}");
        }

    }
}
