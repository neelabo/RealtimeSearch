// Copyright (c) 2015 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RealtimeSearch
{
    /// <summary>
    /// 検索コア
    /// 検索フォルダのファイルをインデックス化して保存し、検索を行う
    /// </summary>
    public class SearchCore
    {
        private List<string> _Roots;

        private Dictionary<string, FileIndex> _FileIndexDirectory;

        private string[] _Keys;

        public List<File> SearchResult { get; private set; }


        public SearchCore()
        {
            _Roots = new List<string>();
            _FileIndexDirectory = new Dictionary<string, FileIndex>();
        }


        /// <summary>
        /// 検索フォルダのインデックス化
        /// </summary>
        /// <param name="roots">検索フォルダ群</param>
        public void Collect(string[] roots)
        {
            // 他のパスに含まれるなら除外
            _Roots.Clear();
            foreach (var path in roots)
            {
                if (!roots.Any(p => path != p && path.StartsWith(p.TrimEnd('\\') + "\\")))
                {
                    _Roots.Add(path);
                }
            }

            Collect();
        }


        /// <summary>
        /// 検索フォルダのインデックス化
        /// 更新分のみ
        /// </summary>
        public void Collect()
        {
            var newDinctionary = new Dictionary<string, FileIndex>();

            foreach (var root in _Roots)
            {
                FileIndex sub;

                if (!_FileIndexDirectory.ContainsKey(root))
                {
                    sub = new FileIndex(root);
                }
                else
                {
                    sub = _FileIndexDirectory[root];
                }

                sub.Collect();
                newDinctionary.Add(root, sub);
            }

            // 再登録されなかったパスの後処理を行う
            foreach (var a in _FileIndexDirectory)
            {
                if (!newDinctionary.ContainsValue(a.Value))
                {
                    a.Value.Dispose();
                }
            }

            _FileIndexDirectory = newDinctionary;
        }


        /// <summary>
        /// インデックス追加
        /// </summary>
        /// <param name="root">検索フォルダ</param>
        /// <param name="paths">追加パス</param>
        public void AddPath(string root, List<string> paths)
        {
            if (!_FileIndexDirectory.ContainsKey(root))
            {
                return;
            }

            foreach (var path in paths)
            {
                _FileIndexDirectory[root].Add(path);
            }

            // 重複除外
            _FileIndexDirectory[root].Distinct();
        }


        /// <summary>
        /// インデックス削除
        /// </summary>
        /// <param name="root">検索フォルダ</param>
        /// <param name="path">削除パス</param>
        public void RemovePath(string root, string path)
        {
            if (!_FileIndexDirectory.ContainsKey(root))
            {
                return;
            }

            _FileIndexDirectory[root].Remove(path);
        }


        /// <summary>
        /// 検索キーを設定する
        /// </summary>
        /// <param name="source">検索キーの元</param>
        public void SetKeys(string source)
        {
            const string splitter = @"[\s]+";

            // 入力文字列を整列
            // 空白、改行文字でパーツ分け
            string s = new Regex("^" + splitter).Replace(source, "");
            s = new Regex(splitter + "$").Replace(s, "");
            this._Keys = new Regex(splitter).Split(s);


            for (int i = 0; i < _Keys.Length; ++i)
            {
                var t = File.ToNormalisedWord(_Keys[i]);

                // 正規表現記号をエスケープ
                t = Regex.Escape(t);

                // (数値)部分を0*(数値)という正規表現に変換
                t = new Regex(@"0*(\d+)").Replace(t, match => "0*" + match.Groups[1]);

                _Keys[i] = (t == "") ? "^$" : t;
            }
        }


        /// <summary>
        /// すべてのFileIndexを走査
        /// </summary>
        /// <returns></returns>
        private IEnumerable<File> AllFiles()
        {
            foreach (var part in _FileIndexDirectory)
            {
                foreach (var file in part.Value.Files)
                    yield return file;
            }
        }


        /// <summary>
        /// 検索
        /// </summary>
        /// <param name="keyword">検索キーワード</param>
        public void Search(string keyword)
        {
            SetKeys(keyword);
            Search();
        }


        /// <summary>
        /// 検索
        /// </summary>
        public void Search()
        {
            if (_Keys == null || _Keys[0] == "^$")
            {
                SearchResult = new List<File>();
                return;
            }

            var entrys = AllFiles();
            foreach (var key in _Keys)
            {
                var regex = new Regex(key, RegexOptions.Compiled);

                var list = new List<File>();
                foreach (var file in entrys)
                {
                    if (regex.Match(file.NormalizedWord).Success)
                    {
                        list.Add(file);
                    }
                }
                entrys = list;
            }

            SearchResult = entrys.ToList();
        }
    }
}
