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

        private FileSystemWatcher _FileSystemWatcher;

        public FileIndex(string path)
        {
            Path = path;
            Files = new List<File>();
            IsDarty = true;

            _FileSystemWatcher = new FileSystemWatcher();
            _FileSystemWatcher.Path = Path;
            _FileSystemWatcher.IncludeSubdirectories = true;
            _FileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;
            _FileSystemWatcher.Created += Watcher_Created;
            _FileSystemWatcher.Deleted += Watcher_Deleted;
            _FileSystemWatcher.Renamed += Watcher_Renamed;
        }


        public void Collect()
        {
            if (!IsDarty) return;
            IsDarty = false;

            // フォルダ監視開始
            _FileSystemWatcher.EnableRaisingEvents = true;

            Files.Clear();
            Add(Path);
        }


        // 追加
        public void Add(string path)
        {
            try
            {
                var entryFile = new File() { Path = path };
                Files.Add(entryFile);

                if (Directory.Exists(path))
                {
                    entryFile.IsDirectory = true;

                    var files = Directory.GetFiles(path).ToList();
                    files.Sort(Win32Api.StrCmpLogicalW);
                    foreach (string file in files)
                    {
                        Files.Add(new File() { Path = file });
                    }

                    var directories = Directory.GetDirectories(path).ToList();
                    directories.Sort(Win32Api.StrCmpLogicalW);
                    foreach (string directory in directories)
                    {
                        Add(directory);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        // 削除
        public void Remove(string path)
        {
            Debug.WriteLine(Files.Count);

            string dir = path + "\\";
            Files.RemoveAll(f => f.Path == path || f.Path.StartsWith(dir));

            Debug.WriteLine(Files.Count);
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
            SearchEngine.Current.RemoveIndexRequest(Path, e.OldFullPath);
            SearchEngine.Current.AddIndexRequest(Path, e.FullPath);
        }

        public void Dispose()
        {
            _FileSystemWatcher.Dispose();
        }
    }
}
