//#define LOCAL_DEBUG

using MemoryPack;


namespace NeeLaboratory.IO.Search.Files
{
    [MemoryPackable]
    public partial class FileNodeMemento
    {
        public FileNodeMemento(int depth, bool isDirectory, string name, DateTime lastWriteTime, long size)
        {
            Depth = depth;
            IsDirectory = isDirectory;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            LastWriteTime = lastWriteTime;
            Size = size;
        }

        public int Depth { get; set; }
        public bool IsDirectory { get; set; }
        public string Name { get; set; }
        public DateTime LastWriteTime { get; set; }
        public long Size { get; set; }
    }
}
