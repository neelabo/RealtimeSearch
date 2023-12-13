//#define LOCAL_DEBUG
using NeeLaboratory.IO.Nodes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NeeLaboratory.RealtimeSearch
{
    public class FileItemTree : FileTree, IFileItemTree
    {
        public FileItemTree(string path, EnumerationOptions enumerationOptions) : base(path, enumerationOptions)
        {
        }


        public event EventHandler<FileTreeContentChangedEventArgs>? AddContentChanged;
        public event EventHandler<FileTreeContentChangedEventArgs>? RemoveContentChanged;


        public class FileTreeContentChangedEventArgs : EventArgs
        {
            public FileTreeContentChangedEventArgs(FileItem fileItem)
            {
                FileItem = fileItem;
            }

            public FileItem FileItem { get; }
        }

        protected override void AttachContent(Node? node, FileSystemInfo file)
        {
            if (node == null) return;

            var fileItem = new FileItem(file);
            node.Content = fileItem;
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

        protected override void OnUpdateContent(Node? node, bool isRecursive)
        {
            if (node is null) return;

            if (isRecursive)
            {
                foreach (var n in node.Walk())
                {
                    DetachContent(n);
                    AttachContent(n, CreateFileInfo(n.FullName));
                }
            }
            else
            {
                DetachContent(node);
                AttachContent(node, CreateFileInfo(node.FullName));
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

            var info = CreateFileInfo(node.FullName);
            fileItem = new FileItem(info);
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
