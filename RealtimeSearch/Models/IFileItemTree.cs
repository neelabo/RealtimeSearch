//#define LOCAL_DEBUG
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static NeeLaboratory.RealtimeSearch.FileItemTree;


namespace NeeLaboratory.RealtimeSearch
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
    }

}
