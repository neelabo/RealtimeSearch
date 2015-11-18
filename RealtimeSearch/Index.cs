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
    public class Index
    {
        string[] roots;

        public List<File> files;

        public string[] keys;
        public List<File> matches;

        public Index()
        {
        }

        //
        public Index(Collection<string> paths)
        {
            roots = paths.ToArray<string>();
        }

        //
        public void Initialize(string[] paths)
        {
            roots = paths;
            Initialize();
        }

        //
        public void Initialize()
        {
            files = new List<File>();

            foreach (var root in roots)
            {
                try
                {
                    foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                    {
                        //files.Add(Path.GetFileName(file));
                        files.Add(new File() { Path = file });
                    }
                }
                catch(Exception)
                {
                    // 何もしない
                }
            }
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
            const string splitter = @"[\s\W]+";
            // 入力文字列を整列
            // 空白、改行文字でパーツ分け
            string s = new Regex("^" + splitter).Replace(source, "");
            s = new Regex(splitter + "$").Replace(s, "");
            this.keys = new Regex(splitter).Split(s);

            for (int i = 0; i < keys.Length; ++i )
            {
                keys[i] = File.ToNormalisedWord(keys[i]);
            }
        }

        /// <summary>
        /// 検索メイン
        /// 検索キーに適応するファイルをリストアップする
        /// </summary>
        public void ListUp()
        {
            matches = new List<File>();
            matches.AddRange(files);

            foreach(var key in keys)
            {
                var list = new List<File>();
                foreach(var file in matches)
                {
                    if (file.NormalizedWord.IndexOf(key) >= 0)
                    {
                        list.Add(file);
                    }
                }
                matches = list;
            }
        }

    }
}
