//#define LOCAL_DEBUG

using MemoryPack;


namespace NeeLaboratory.IO.Search.Files
{
    [MemoryPackable(GenerateType.VersionTolerant)]
    public partial class FileTreeMemento
    {
        public FileTreeMemento(FileArea fileArea, List<FileNodeMemento> nodes)
        {
            FileArea = fileArea ?? throw new ArgumentNullException(nameof(fileArea));
            Nodes = nodes ?? new();
        }

        [MemoryPackOrder(0)]
        public FileArea FileArea { get; set; }

        [MemoryPackOrder(1)]
        public List<FileNodeMemento> Nodes { get; set; }
    }
}
