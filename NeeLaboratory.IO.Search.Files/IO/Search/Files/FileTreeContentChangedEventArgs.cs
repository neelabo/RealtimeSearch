//#define LOCAL_DEBUG
using System;


namespace NeeLaboratory.IO.Search.Files
{
    public class FileTreeContentChangedEventArgs : EventArgs
    {
        public FileTreeContentChangedEventArgs(FileContent content)
            : this(content, null)
        {
        }

        public FileTreeContentChangedEventArgs(FileContent content, FileContent? oldContent)
        {
            Content = content;
            OldContent = oldContent;
        }

        public FileContent Content { get; }
        public FileContent? OldContent { get; }
    }

}
