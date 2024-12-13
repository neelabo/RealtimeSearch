using NeeLaboratory.IO.Search.Files;

namespace NeeLaboratory.RealtimeSearch
{
    public class RenameItemMessage
    {
        public RenameItemMessage(FileContent item)
        {
            Item = item;
        }

        public FileContent Item { get; }
    }

}
