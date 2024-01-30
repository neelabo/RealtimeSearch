using NeeLaboratory.IO.Search.Files;

namespace NeeLaboratory.RealtimeSearch
{
    public class RenameItemMessage
    {
        public RenameItemMessage(FileItem item)
        {
            Item = item;
        }

        public FileItem Item { get; }
    }

}
