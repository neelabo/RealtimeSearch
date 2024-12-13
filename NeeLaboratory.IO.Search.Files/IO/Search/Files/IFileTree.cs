//#define LOCAL_DEBUG
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace NeeLaboratory.IO.Search.Files
{
    public interface IFileTree
    {
        event EventHandler<FileTreeContentChangedEventArgs>? AddContentChanged;
        event EventHandler<FileTreeContentChangedEventArgs>? RemoveContentChanged;
        event EventHandler<FileTreeContentChangedEventArgs>? ContentChanged;

        int Count { get; }

        void Initialize(CancellationToken token);
        Task InitializeAsync(CancellationToken token);
        Task<IDisposable> LockAsync(CancellationToken token);
        IEnumerable<FileContent> CollectFileContents();

        void Wait(CancellationToken token);
        Task WaitAsync(CancellationToken token);
    }
}
