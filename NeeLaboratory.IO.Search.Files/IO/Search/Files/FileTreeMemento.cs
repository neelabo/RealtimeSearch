//#define LOCAL_DEBUG

using MemoryPack;


namespace NeeLaboratory.IO.Search.Files
{
    [MemoryPackable]
    public partial class FileTreeMemento
    {
        public FileTreeMemento(FileArea fileArea, List<FileNodeMemento> nodes)
        {
            FileArea = fileArea ?? throw new ArgumentNullException(nameof(fileArea));
            Nodes = nodes ?? new();
        }

        public FileArea FileArea { get; set; }
        public List<FileNodeMemento> Nodes { get; set; }
    }
}
