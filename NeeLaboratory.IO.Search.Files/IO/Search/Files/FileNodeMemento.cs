using MemoryPack;

namespace NeeLaboratory.IO.Search.Files
{
    [MemoryPackable(GenerateType.VersionTolerant)]
    public partial class FileNodeMemento
    {
        public FileNodeMemento(int depth, string name) : this(depth, false, name, default, 0)
        {
        }

        [MemoryPackConstructor]
        public FileNodeMemento(int depth, bool isDirectory, string name, DateTime lastWriteTime, long size)
        {
            Depth = depth;
            IsDirectory = isDirectory;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            LastWriteTime = lastWriteTime;
            Size = size;
        }

        [MemoryPackOrder(0)]
        public int Depth { get; set; }

        [MemoryPackOrder(1)]
        public bool IsDirectory { get; set; }

        [MemoryPackOrder(2)]
        public string Name { get; set; }

        [MemoryPackOrder(3)]
        public DateTime LastWriteTime { get; set; }

        [MemoryPackOrder(4)]
        public long Size { get; set; }
    }
}
