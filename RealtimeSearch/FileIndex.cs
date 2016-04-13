// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealtimeSearch
{
    public class FileIndex : IDisposable
    {
        public string Path { get; private set; }

        public List<File> Files { get; private set; }

        public bool IsDarty { get; private set; }

        // ファイル変更監視
        private FileSystemWatcher _FileSystemWatcher;

        public FileIndex(string path)
        {
            Path = path;
            Files = new List<File>();
            IsDarty = true;

            _FileSystemWatcher = new FileSystemWatcher();
            _FileSystemWatcher.Path = Path;
            _FileSystemWatcher.IncludeSubdirectories = true;
            _FileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size;
            _FileSystemWatcher.Created += Watcher_Created; 
            _FileSystemWatcher.Deleted += Watcher_Deleted;
            _FileSystemWatcher.Renamed += Watcher_Renamed;
            _FileSystemWatcher.Changed += Watcher_Changed;
        }


        public void Collect()
        {
            if (!IsDarty) return;
            IsDarty = false;

            // フォルダ監視開始
            _FileSystemWatcher.EnableRaisingEvents = true;

            Files.Clear();
            Add(Path,  false);
        }


        // 追加
        public List<File> Add(string path, bool isCheckDuplicate)
        {
            var addedFiles = new List<File>();

            try
            {
                if (isCheckDuplicate)
                {
                    if (Files.Any(f => f.Path == path))
                    {
                        return addedFiles;
                    }
                }

                var entryFile = new File() { Path = path };
                Files.Add(entryFile);
                addedFiles.Add(entryFile);

                if (Directory.Exists(path))
                {
                    entryFile.IsDirectory = true;

                    var files = Directory.GetFiles(path).ToList();
                    files.Sort(Win32Api.StrCmpLogicalW);
                    foreach (string file in files)
                    {
                        var item = new File() { Path = file };
                        Files.Add(item);
                        addedFiles.Add(item);
                    }

                    var directories = Directory.GetDirectories(path).ToList();
                    directories.Sort(Win32Api.StrCmpLogicalW);
                    foreach (string directory in directories)
                    {
                        addedFiles.AddRange(Add(directory, isCheckDuplicate));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            return addedFiles;
        }

        // 削除
        public List<File> Remove(string path)
        {
            //Debug.WriteLine(Files.Count);

            string dir = path + "\\";

            List<File> removeFiles = Files.Where(f => f.Path == path || f.Path.StartsWith(dir)).ToList();
            Files.RemoveAll(f => removeFiles.Contains(f));

            //Debug.WriteLine(Files.Count);
            return removeFiles;
        }

        // 更新
        public void RefleshIndex(string path)
        {
            var file = Files.Find(f => f.Path == path);
            if (file == null) return;

            file.Reflesh();
        }



        class FileComparer : EqualityComparer<File>
        {
            public override bool Equals(File p1, File p2)
            {
                return (p1.Path == p2.Path);
            }

            public override int GetHashCode(File p)
            {
                return p.Path.GetHashCode();
            }
        }

        public void Distinct()
        {
            Files = Files.Distinct(new FileComparer()).ToList();
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            SearchEngine.Current.AddIndexRequest(Path, e.FullPath);
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            SearchEngine.Current.RemoveIndexRequest(Path, e.FullPath);
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            SearchEngine.Current.RenameIndexRequest(Path, e.OldFullPath, e.FullPath);
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            SearchEngine.Current.RefleshIndexRequest(Path, e.FullPath);
        }


        public void Dispose()
        {
            _FileSystemWatcher.Dispose();
        }
    }
}
