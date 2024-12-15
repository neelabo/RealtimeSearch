using MemoryPack;


namespace NeeLaboratory.IO.Search.Files
{
    [MemoryPackable(GenerateType.VersionTolerant)]
    public partial class FileForestMemento
    {
        public FileForestMemento(List<FileTreeMemento> trees)
        {
            Trees = trees ?? new();
        }

        [MemoryPackOrder(0)]
        public List<FileTreeMemento> Trees { get; set; }
    }

}
