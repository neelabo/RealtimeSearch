// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public ObservableCollection<File> SearchResult { get; private set; }


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SearchCore()
        {
            _Roots = new List<string>();
            _FileIndexDirectory = new Dictionary<string, FileIndex>();
            SearchResult = new ObservableCollection<File>();
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
        public List<File> AddPaths(string root, List<string> paths)
        {
            var newFiles = new List<File>();

            if (!_FileIndexDirectory.ContainsKey(root))
            {
                return newFiles;
            }

            foreach (var path in paths)
            {
                newFiles.AddRange(_FileIndexDirectory[root].Add(path, true));
            }

            return newFiles;
        }


        /// <summary>
        /// インデックス削除
        /// </summary>
        /// <param name="root">検索フォルダ</param>
        /// <param name="path">削除パス</param>
        public List<File> RemovePath(string root, string path)
        {
            if (!_FileIndexDirectory.ContainsKey(root))
            {
                return new List<File>();
            }

            return _FileIndexDirectory[root].Remove(path);
        }

        /// <summary>
        /// インデックスの情報更新
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        public void RefleshIndex(string root, string path)
        {
            if (!_FileIndexDirectory.ContainsKey(root))
            {
                return;
            }

            _FileIndexDirectory[root].RefleshIndex(path);
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
        /// 検索 -- 検索結果を更新する
        /// </summary>
        /// <param name="keyword">検索キーワード</param>
        /// <param name="isSearchFolder">フォルダを検索対象に含めるフラグ</param>
        public void UpdateSearchResult(string keyword, bool isSearchFolder)
        {
            SearchResult = new ObservableCollection<File>(Search(keyword, AllFiles(), isSearchFolder));
        }


        /// <summary>
        /// 検索
        /// </summary>
        /// <param name="keyword">検索キーワード</param>
        /// <param name="entries">検索対象</param>
        /// <param name="isSearchFolder">フォルダを検索対象に含めるフラグ</param>
        /// <returns></returns>
        public IEnumerable<File> Search(string keyword, IEnumerable<File> entries, bool isSearchFolder)
        {
            // pushpin保存
            var pushpins = entries.Where(f => f.IsPushPin);

            // キーワード登録
            SetKeys(keyword);
            if (_Keys == null || _Keys[0] == "^$")
            {
                return pushpins;
            }

            // キーワードによる絞込
            foreach (var key in _Keys)
            {
                var regex = new Regex(key, RegexOptions.Compiled);
                entries = entries.Where(f => regex.Match(f.NormalizedWord).Success);
            }

            // ディレクトリ除外
            if (!isSearchFolder)
            {
                entries = entries.Where(f => !f.IsDirectory);
            }

            // pushpin除外
            entries = entries.Where(f => !f.IsPushPin);

            // pushpinを先頭に連結して返す
            return pushpins.Concat(entries);
        }

    }
}
