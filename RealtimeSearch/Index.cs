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
            //const string splitter = @"[\s\W]+"; // 記号も無視
            const string splitter = @"[\s]+";

            // 入力文字列を整列
            // 空白、改行文字でパーツ分け
            string s = new Regex("^" + splitter).Replace(source, "");
            s = new Regex(splitter + "$").Replace(s, "");
            this.keys = new Regex(splitter).Split(s);

            for (int i = 0; i < keys.Length; ++i )
            {
                var t = File.ToNormalisedWord(keys[i]);

                // 正規表現記号をエスケープ
                t = Regex.Escape(t);

                // (数値)部分を0*(数値)という正規表現に変換
                t = new Regex(@"0*(\d+)").Replace(t, match => "0*" + match.Groups[1]);

                keys[i] = t;
            }
        }

        /// <summary>
        /// 検索メイン
        /// 検索キーに適応するファイルをリストアップする
        /// </summary>
        public void ListUp()
        {
            var entrys = files;

            foreach (var key in keys)
            {
                var regex = new Regex(key, RegexOptions.Compiled);

                var list = new List<File>();
                foreach(var file in entrys)
                {
                    //if (file.NormalizedWord.IndexOf(key) >= 0)
                    if (regex.Match(file.NormalizedWord).Success)
                    {
                        list.Add(file);
                    }
                }
                entrys = list;
            }

            matches = entrys;
        }
    }
}
