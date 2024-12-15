using MemoryPack;
using System;
using System.Runtime.Serialization;

namespace NeeLaboratory.IO.Search.Files
{
    [MemoryPackable(GenerateType.VersionTolerant)]
    public partial record class FileArea
    {
        public FileArea()
        {
        }

        public FileArea(string path)
        {
            Path = System.IO.Path.GetFullPath(path);
        }

        [MemoryPackConstructor]
        public FileArea(string path, bool includeSubdirectories) : this(path)
        {
            IncludeSubdirectories = includeSubdirectories;
        }

        [MemoryPackOrder(0)]
        public string Path { get; init; } = "";

        [MemoryPackOrder(1)]
        public bool IncludeSubdirectories { get; init; }

        public string GetName()
        {
            return LoosePath.GetFileName(Path);
        }

        public bool Contains(FileArea other)
        {
            if (this == other) return false;

            if (this.Path == other.Path)
            {
                return this.IncludeSubdirectories || other.IncludeSubdirectories == false;
            }

            if (this.Path.Length > other.Path.Length)
            {
                return false;
            }

            if (IncludeSubdirectories)
            {
                var directoryName = LoosePath.TrimDirectoryEnd(Path);
                return other.Path.StartsWith(directoryName);
            }
            else
            {
                return false;
            }
        }

    }

}
