//#define LOCAL_DEBUG

namespace NeeLaboratory.IO.Search.Files
{
    public class FileTreeCollectBusyChangedEventArgs : EventArgs
    {
        public FileTreeCollectBusyChangedEventArgs(bool isCollectBusy)
        {
            IsCollectBusy = isCollectBusy;
        }

        public bool IsCollectBusy { get; }
    }

}
