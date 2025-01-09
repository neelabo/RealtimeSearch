//#define LOCAL_DEBUG
using NeeLaboratory.Generators;
using NeeLaboratory.Native;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

namespace NeeLaboratory.IO.Search.Files
{
    [NotifyPropertyChanged]
    public partial class FileContent : ISearchItem, IComparable, IComparable<FileContent>, INotifyPropertyChanged
    {
        private FileContentState _state;


        public FileContent(FileSystemInfo fileSystemInfo)
        {
            SetFileInfo(fileSystemInfo);
        }

        public FileContent(bool isDirectory, string path, string name, DateTime lastWriteTime, long size, FileContentState state)
        {
            IsDirectory = isDirectory;
            Path = path;
            Name = name;
            Size = size;
            LastWriteTime = lastWriteTime;
            State = state;
        }


        public event PropertyChangedEventHandler? PropertyChanged;


        public bool IsDirectory { get; private set; }
        public string Path { get; private set; }
        public string Name { get; private set; }
        public long Size { get; private set; }
        public DateTime LastWriteTime { get; private set; }

        public FileContentState State
        {
            get { return _state; }
            set { SetProperty(ref _state, value); }
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

        /// <summary>
        /// Pushpinフラグ
        /// </summary>
        public bool IsPushpin { get; set; }


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
        [MemberNotNull(nameof(Path), nameof(Name))]
        public void SetFileInfo(FileSystemInfo fileSystemInfo)
        {
            Path = fileSystemInfo.FullName;
            Name = fileSystemInfo.Name;

            if (fileSystemInfo.Exists)
            {
                try
                {
                    IsDirectory = fileSystemInfo.Attributes.HasFlag(FileAttributes.Directory);
                    LastWriteTime = fileSystemInfo.LastWriteTime;
                    Size = fileSystemInfo is FileInfo fileInfo ? fileInfo.Length : -1;

                    // ディレクトリで不明の場合は、子の情報が不明な状態にする
                    State = (IsDirectory && State.IsUnknown()) ? FileContentState.UnknownChildren : FileContentState.Stable;
                }
                catch (FileNotFoundException)
                {
                    Debug.WriteLine($"SetFileInfo: File not found: {Path}");
                }
            }
            else
            {
                Debug.WriteLine($"SetFileInfo: File not found: {Path}");
            }

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
            return CompareTo(obj as FileContent);
        }

        public int CompareTo(FileContent? other)
        {
            if (other == null) return 1;
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
                    return new BooleanSearchValue(IsPushpin);
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
