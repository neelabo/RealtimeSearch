using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Diagnostics;

using Microsoft.VisualBasic;

namespace RealtimeSearch
{
    //
    public class IndexSubset : IDisposable
    {
        public string Path { get; private set; }

        public List<File> Files { get; private set; }

        public bool IsDarty { get; private set; }

        private FileSystemWatcher FileSystemWatcher;


        public IndexSubset(string path)
        {
            Path = path;
            Files = new List<File>();
            IsDarty = true;

            FileSystemWatcher = new FileSystemWatcher();
            FileSystemWatcher.Path = Path;
            FileSystemWatcher.IncludeSubdirectories = true;
            FileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;
            FileSystemWatcher.Created += Watcher_Created;
            FileSystemWatcher.Deleted += Watcher_Deleted;
            FileSystemWatcher.Renamed += Watcher_Renamed;
        }


        public void Collect()
        {
            if (!IsDarty) return;
            IsDarty = false;

            // フォルダ監視開始
            FileSystemWatcher.EnableRaisingEvents = true;

            Files.Clear();
            Add(Path);
        }


        // 追加
        public void Add(string path)
        {
            try
            {
                Files.Add(new File() { Path = path });

                if (Directory.Exists(path))
                {
                    foreach (string file in Directory.GetFiles(path))
                    {
                        Files.Add(new File() { Path = file });
                    }
                    foreach (string directory in Directory.GetDirectories(path))
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

/*
        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            IsDarty = true;
            SearchEngine.Current.ReIndexRequest();
        }
*/

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
            FileSystemWatcher.Dispose();
        }
    }




    //
    public class Index
    {
        string[] roots;

        public Dictionary<string, IndexSubset> IndexDictionary { get; private set; }

        public string[] keys;
        public List<File> matches;

        public Index()
        {
            IndexDictionary = new Dictionary<string, IndexSubset>();
        }


        //
        public void Collect(string[] paths)
        {
            roots = paths;
            Collect();
        }

        //
        public void Collect()
        {
            var newDinctionary = new Dictionary<string, IndexSubset>();

            foreach (var root in roots)
            {
                IndexSubset sub;

                if (!IndexDictionary.ContainsKey(root))
                {
                    sub = new IndexSubset(root);
                }
                else
                {
                    sub = IndexDictionary[root];
                }

                sub.Collect();
                newDinctionary.Add(root, sub);
            }

            // 再登録されなかったパスの後処理を行う
            foreach (var a in IndexDictionary)
            {
                if (!newDinctionary.ContainsValue(a.Value))
                {
                    a.Value.Dispose();
                }
            }

            IndexDictionary = newDinctionary;
        }


        public void AddPath(string root, List<string> paths)
        {
            if (!IndexDictionary.ContainsKey(root))
            {
                return;
            }

            foreach (var path in paths)
            {
                // これが重い
                //if (IndexDictionary[root].Files.Any(f => f.Path == path)) continue;

                IndexDictionary[root].Add(path);
            }

            //IndexDictionary[root].Files.Distinct()

            IndexDictionary[root].Distinct();
        }


        public void RemovePath(string root, string path)
        {
            if (!IndexDictionary.ContainsKey(root))
            {
                return;
            }

            IndexDictionary[root].Remove(path);
        }



        /// <summary>
        /// 検索
        /// </summary>
        /// <param name="source">検索キーの元</param>
        public void Check(string source)
        {
            SetKeys(source);
            ListUp();
        }

        /// <summary>
        /// 検索キーを設定する
        /// </summary>
        /// <param name="source">検索キーの元</param>
        public void SetKeys(string source)
        {
            //const string splitter = @"[\s\W]+"; // 記号も無視
            const string splitter = @"[\s]+";

            // 入力文字列を整列
            // 空白、改行文字でパーツ分け
             string s = new Regex("^" + splitter).Replace(source, "");
            s = new Regex(splitter + "$").Replace(s, "");
            this.keys = new Regex(splitter).Split(s);

#if false
            if (string.IsNullOrEmpty(keys[0]))
            {
                keys = null;
                return;
            }
#endif

            for (int i = 0; i < keys.Length; ++i )
            {
                var t = File.ToNormalisedWord(keys[i]);

                // 正規表現記号をエスケープ
                t = Regex.Escape(t);

                // (数値)部分を0*(数値)という正規表現に変換
                t = new Regex(@"0*(\d+)").Replace(t, match => "0*" + match.Groups[1]);

                keys[i] = (t == "") ? "^$" : t;
            }
        }


        private IEnumerable<File> AllFiles()
        {
            foreach(var part in IndexDictionary)
            {
                foreach (var file in part.Value.Files)
                    yield return file;
            }
        }

        /// <summary>
        /// 検索メイン
        /// 検索キーに適応するファイルをリストアップする
        /// </summary>
        public void ListUp()
        {
            if (keys == null || keys[0] == "^$")
            {
                matches = new List<File>();
                return;
            }


            var entrys = AllFiles();
            foreach (var key in keys)
            {
                var regex = new Regex(key, RegexOptions.Compiled);

                var list = new List<File>();
                foreach(var file in entrys)
                {
                    if (regex.Match(file.NormalizedWord).Success)
                    {
                        list.Add(file);
                    }
                }
                entrys = list;
            }

            matches = entrys.ToList();
        }
    }
}
