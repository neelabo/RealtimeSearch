//#define LOCAL_DEBUG
using NeeLaboratory.Generators;
using NeeLaboratory.IO.Search;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace NeeLaboratory.IO.Search.Files
{
    [NotifyPropertyChanged]
    public partial class FileItem : ISearchItem, IComparable, INotifyPropertyChanged
    {
        internal static partial class NativeMethods
        {
            // 参考：自然順ソート
            [LibraryImport("shlwapi.dll", EntryPoint = "StrCmpLogicalW", StringMarshalling = StringMarshalling.Utf16)]
            public static partial int StrCmpLogicalW(string psz1, string psz2);
        }


        private bool _isDirty;


        public FileItem(FileSystemInfo fileSystemInfo)
        {
            SetFileInfo(fileSystemInfo);
        }

        public FileItem(bool isDirectory, string path, string name, DateTime lastWriteTime, long size, bool isDirty)
        {
            IsDirectory = isDirectory;
            Path = path;
            Name = name;
            Size = size;
            LastWriteTime = lastWriteTime;
            IsDirty = isDirty;
        }


        public event PropertyChangedEventHandler? PropertyChanged;


        public bool IsDirectory { get; private set; }
        public string Path { get; private set; }
        public string Name { get; private set; }
        public long Size { get; private set; }
        public DateTime LastWriteTime { get; private set; }

        public bool IsDirty
        {
            get { return _isDirty; }
            set { SetProperty(ref _isDirty, value); }
        }


        public string? Extension
        {
            get { return IsDirectory ? null : System.IO.Path.GetExtension(Name); }
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


        /// <summary>
        /// Update FileInfo
        /// </summary>
        /// <param name="fullName">Path</param>
        public void SetFileInfo(string fullName)
        {
            SetFileInfo(CreateFileSystemInfo(fullName));
        }

        /// <summary>
        /// Update FileInfo
        /// </summary>
        /// <param name="fileSystemInfo"></param>
        [MemberNotNull(nameof(IsDirectory), nameof(Path), nameof(Name), nameof(Size), nameof(LastWriteTime))]
        public void SetFileInfo(FileSystemInfo fileSystemInfo)
        {
            IsDirectory = fileSystemInfo.Attributes.HasFlag(FileAttributes.Directory);
            Path = fileSystemInfo.FullName;
            Name = fileSystemInfo.Name;
            Size = fileSystemInfo is FileInfo fileInfo ? fileInfo.Length : -1;
            LastWriteTime = fileSystemInfo.LastWriteTime;
            IsDirty = false;

            RaisePropertyChanged(null);
        }

        public static FileSystemInfo CreateFileSystemInfo(string path)
        {
            var directoryInfo = new DirectoryInfo(path);
            if (directoryInfo.Exists) return directoryInfo;
            return new FileInfo(path);
        }

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
                    return new StringSearchValue(Name);
                case "date":
                    return new DateTimeSearchValue(LastWriteTime);
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
