//#define LOCAL_DEBUG
using MemoryPack;


namespace NeeLaboratory.IO.Search.Files
{
    [MemoryPackable]
    public partial class FileForestMemento
    {
        public FileForestMemento(List<FileTreeMemento> trees)
        {
            Trees = trees ?? new();
        }

        public List<FileTreeMemento> Trees { get; set; }
    }

}
