//#define LOCAL_DEBUG
using System;


namespace NeeLaboratory.IO.Search.Files
{
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

}
