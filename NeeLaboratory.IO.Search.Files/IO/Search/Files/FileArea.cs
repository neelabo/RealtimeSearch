using System;
using System.Runtime.Serialization;

namespace NeeLaboratory.IO.Search.Files
{
    public class FileArea
    {
        public FileArea()
        {
        }

        public FileArea(string path)
        {
            Path = System.IO.Path.GetFullPath(path);
        }

        public FileArea(string path, bool includeSubdirectories) : this(path)
        {
            IncludeSubdirectories = includeSubdirectories;
        }

        public string Path { get; set; } = "";

        public bool IncludeSubdirectories { get; set; }


        public bool Contains(FileArea other)
        {
            if (this == other) return false;

            if (this.Path == other.Path) return true;

            if (this.Path.Length > other.Path.Length) return false;

            if (IncludeSubdirectories)
            {
                if (other.Path.StartsWith(this.Path))
                {
                    return other.Path[Path.Length] == '\\';
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (other.IncludeSubdirectories)
                {
                    return false;
                }
                else
                {
                    return Path == other.Path;
                }
            }
        }
    }

}
