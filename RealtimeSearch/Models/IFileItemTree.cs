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

        Task InitializeAsync(CancellationToken token);
        Task<IDisposable> LockAsync(CancellationToken token);
        IEnumerable<FileItem> CollectFileItems();
        Task WaitAsync(CancellationToken token);

        void RequestRename(string src, string dst);
    }

}
