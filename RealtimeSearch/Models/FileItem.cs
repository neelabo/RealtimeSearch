//#define LOCAL_DEBUG
using NeeLaboratory.IO.Search;
using NeeLaboratory.IO.Search.FileNode;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace NeeLaboratory.RealtimeSearch
{
    public class FileItem : ISearchItem, IComparable
    {
        internal static class NativeMethods
        {
            // 参考：自然順ソート
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
            public static extern int StrCmpLogicalW(string psz1, string psz2);
        }

        private FileSystemInfo _info;

        public FileItem(FileSystemInfo fileSystemInfo)
        {
            _info = fileSystemInfo;
        }

        public FileSystemInfo FileSystemInfo => _info;

        public bool IsDirectory => _info.Attributes.HasFlag(FileAttributes.Directory);

        public string Path => _info.FullName;

        public DateTime LastWriteTime => _info.LastWriteTime;

        public long Size => _info is System.IO.FileInfo fileInfo ? fileInfo.Length : -1;


        public string Name => _info.Name;

        public string? Extension
        {
            get { return IsDirectory ? null : System.IO.Path.GetExtension(_info.Name); }
        }

        public string? DirectoryName
        {
            get
            {
                string? dir = System.IO.Path.GetDirectoryName(Path);
                string? parentDir = System.IO.Path.GetDirectoryName(dir);
                return parentDir == null ? dir : System.IO.Path.GetFileName(dir) + " (" + parentDir + ")";
            }
        }

        public string Detail
        {
            get
            {
                string sizeText = Size >= 0 ? $"Size: {(Size + 1024 - 1) / 1024:#,0} KB\n" : "Size: --\n";
                var dateText = LastWriteTime.ToString(SearchDateTimeTools.DateTimeFormat);
                return $"{Name}\n{sizeText}Date: {dateText}\nFolder: {DirectoryName}";
            }
        }

        /// <summary>
        /// PushPinフラグ
        /// </summary>
        public bool IsPushPin { get; set; }


        public int CompareTo(object? obj)
        {
            if (obj == null) return 1;

            FileItem other = (FileItem)obj;
            return NativeMethods.StrCmpLogicalW(Name, other.Name);
        }

        public SearchValue GetValue(SearchPropertyProfile profile, string? parameter, CancellationToken token)
        {
            switch (profile.Name)
            {
                case "text":
                    return new StringSearchValue(_info.Name);
                case "date":
                    return new DateTimeSearchValue(_info.LastWriteTime);
                case "size":
                    return new IntegerSearchValue(Size);
                case "directory":
                    return new BooleanSearchValue(IsDirectory);
                case "pinned":
                    return new BooleanSearchValue(IsPushPin);
                default:
                    throw new NotSupportedException();
            }
        }

        public override string ToString()
        {
            return Path;
        }
    }

}
