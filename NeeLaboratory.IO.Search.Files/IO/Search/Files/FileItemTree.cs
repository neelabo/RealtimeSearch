//#define LOCAL_DEBUG

using NeeLaboratory.Collections;
using NeeLaboratory.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;


namespace NeeLaboratory.IO.Search.Files
{
    public class FileItemTree : FileTree, IFileItemTree
    {
        private FileArea _area;

        public FileItemTree(FileArea area, FileTreeMemento? memento) : base(area.Path, memento, CreateEnumerationOptions(area))
        {
            _area = area;

            // すべての Node の Content を保証する
            foreach (var node in Root.Walk())
            {
                if (string.IsNullOrEmpty(node.Name)) continue;
                EnsureContent(node);
            }
        }


        public event EventHandler<FileTreeContentChangedEventArgs>? AddContentChanged;
        public event EventHandler<FileTreeContentChangedEventArgs>? RemoveContentChanged;
        public event EventHandler<FileTreeContentChangedEventArgs>? ContentChanged;

        public FileArea Area => _area;


        private static EnumerationOptions CreateEnumerationOptions(FileArea area)
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
            Debug.Assert(node.Content is null);

            var info = CreateFileInfo(node.FullName);
            var fileItem = new FileItem(info);
            node.Content = fileItem;
            return fileItem;
        }

        private void EnsureContent(Node node)
        {
            if (node.Content is null)
            {
                node.Content = new FileItem(CreateFileInfo(node.FullName));
            }
        }

        public void DumpDepth()
        {
            foreach (var item in Trunk.WalkChildrenWithDepth())
            {
                var fileItem = GetFileItem(item.Node);
                var fileNode = new FileNodeMemento(item.Depth, fileItem.IsDirectory, item.Node.Name, fileItem.LastWriteTime, fileItem.Size);
                Debug.WriteLine(fileNode);
            }
        }

        #region Memento
        // TODO: Memento が FileItemTree と等しく対応していないので、データ構造の見直しが必要

        public FileTreeMemento CreateTreeMemento()
        {
            // TODO: Trunk から生成してるけど、Root からでよくないか？
            return new FileTreeMemento(_area, CreateTreeMemento(Trunk));
        }

        public List<FileNodeMemento> CreateTreeMemento(Node node)
        {
            return node.WalkWithDepth().Select(e => CreateFileNode(e.Node, e.Depth)).ToList();
        }

        private FileNodeMemento CreateFileNode(Node node, int depth)
        {
            var fileItem = GetFileItem(node);
            return new FileNodeMemento(depth, fileItem.IsDirectory, node.Name, fileItem.LastWriteTime, fileItem.Size);
        }

        public static Node? RestoreTree(FileTreeMemento memento)
        {
            if (memento == null) return null;
            if (memento.FileArea is null) return null;
            if (memento.Nodes is null) return null;

            var directory = System.IO.Path.GetDirectoryName(memento.FileArea.Path) ?? "";

            Node root = new Node("");
            Node current = root;
            int depth = -1;
            foreach (var node in memento.Nodes)
            {
                Debug.Assert(node.Depth > 0 || System.IO.Path.GetFileName(memento.FileArea.Path) == node.Name);

                while (node.Depth <= depth)
                {
                    current = current.Parent ?? throw new FormatException("Cannot get parent node.");
                    depth--;
                }
                if (node.Depth - 1 != depth) throw new FormatException("Node is missing.");

                var child = new Node(node.Name);
                current.AddChild(child);
                var path = System.IO.Path.Combine(directory, current.FullName, node.Name);
                child.Content = new FileItem(node.IsDirectory, path, node.Name, node.LastWriteTime, node.Size, true);
                current = child;
                depth = node.Depth;
            }

            // Trunk
            Debug.Assert(root.Children?.Count == 1);
            var trunkNode = root.Children.First();
            root.RemoveChild(trunkNode);

            return trunkNode;
        }

        #endregion


        [Conditional("LOCAL_DEBUG")]
        private void Trace(string s, params object[] args)
        {
            Debug.WriteLine($"{this.GetType().Name}: {string.Format(s, args)}");
        }
    }
}
