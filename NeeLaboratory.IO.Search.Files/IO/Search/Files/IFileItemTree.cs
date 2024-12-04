//#define LOCAL_DEBUG
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace NeeLaboratory.IO.Search.Files
{
    public interface IFileItemTree
    {
        event EventHandler<FileTreeContentChangedEventArgs>? AddContentChanged;
        event EventHandler<FileTreeContentChangedEventArgs>? RemoveContentChanged;
        event EventHandler<FileTreeContentChangedEventArgs>? ContentChanged;

        int Count { get; }

        void Initialize(CancellationToken token);
        Task InitializeAsync(CancellationToken token);
        Task<IDisposable> LockAsync(CancellationToken token);
        IEnumerable<FileItem> CollectFileItems();

        void Wait(CancellationToken token);
        Task WaitAsync(CancellationToken token);

        void ReserveRename(string src, string dst);
    }
}
