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

        private Dictionary<string, NodeTree> _FileIndexDirectory;

        private string[] _Keys;

        public ObservableCollection<NodeContent> SearchResult { get; private set; }


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SearchCore()
        {
            _Roots = new List<string>();
            _FileIndexDirectory = new Dictionary<string, NodeTree>();
            SearchResult = new ObservableCollection<NodeContent>();
        }


        /// <summary>
        /// 検索フォルダのインデックス化
        /// </summary>
        /// <param name="roots">検索フォルダ群</param>
        public void Collect(string[] roots)
        {
            _Roots.Clear();

            // 他のパスに含まれるなら除外
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
            var newDinctionary = new Dictionary<string, NodeTree>();

            foreach (var root in _Roots)
            {
                NodeTree sub;

                if (!_FileIndexDirectory.ContainsKey(root))
                {
                    sub = new NodeTree(root);
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
            System.GC.Collect();
        }


        /// <summary>
        /// インデックス追加
        /// </summary>
        /// <param name="root">検索フォルダ</param>
        /// <param name="paths">追加パス</param>
        public Node AddPath(string root, string path)
        {
            if (!_FileIndexDirectory.ContainsKey(root))
            {
                return null;
            }

            return _FileIndexDirectory[root].AddNode(path);
        }


        /// <summary>
        /// インデックス削除
        /// </summary>
        /// <param name="root">検索フォルダ</param>
        /// <param name="path">削除パス</param>
        public Node RemovePath(string root, string path)
        {
            if (!_FileIndexDirectory.ContainsKey(root))
            {
                return null;
            }

            return _FileIndexDirectory[root].RemoveNode(path);
        }

        /// <summary>
        /// リネーム
        /// </summary>
        /// <param name="root"></param>
        /// <param name="oldFileName"></param>
        /// <param name="newFilename"></param>
        public Node RenamePath(string root, string oldFileName, string newFileName)
        {
            if (!_FileIndexDirectory.ContainsKey(root))
            {
                return null;
            }

            return _FileIndexDirectory[root].Rename(oldFileName, newFileName);
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

            _FileIndexDirectory[root].RefleshNode(path);
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
                var t = Node.ToNormalisedWord(_Keys[i]);

                // 正規表現記号をエスケープ
                t = Regex.Escape(t);

                // (数値)部分を0*(数値)という正規表現に変換
                t = new Regex(@"0*(\d+)").Replace(t, match => "0*" + match.Groups[1]);

                _Keys[i] = (t == "") ? "^$" : t;
            }
        }


        /// <summary>
        /// すべてのNodeを走査
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Node> AllNodes()
        {
            foreach (var part in _FileIndexDirectory)
            {
                foreach (var node in part.Value.Root.AllChildren())
                    yield return node;
            }
        }

        //
        private object _Lock = new object();

        //
        public void ClearSearchResult()
        {
            lock (_Lock)
            {
                SearchResult.Clear();
            }
        }

        /// <summary>
        /// 検索 -- 検索結果を更新する
        /// </summary>
        /// <param name="keyword">検索キーワード</param>
        /// <param name="isSearchFolder">フォルダを検索対象に含めるフラグ</param>
        public void UpdateSearchResult(string keyword, bool isSearchFolder)
        {
            lock (_Lock)
            {
                SearchResult = new ObservableCollection<NodeContent>(Search(keyword, AllNodes(), isSearchFolder).Select(e => e.Content));
            }
        }

        /// <summary>
        /// 検索
        /// </summary>
        /// <param name="keyword">検索キーワード</param>
        /// <param name="entries">検索対象</param>
        /// <param name="isSearchFolder">フォルダを検索対象に含めるフラグ</param>
        /// <returns></returns>
        public IEnumerable<Node> Search(string keyword, IEnumerable<Node> entries, bool isSearchFolder)
        {
            // pushpin保存
            var pushpins = entries.Where(f => f.Content.IsPushPin);

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
            entries = entries.Where(f => !f.Content.IsPushPin);

            // pushpinを先頭に連結して返す
            return pushpins.Concat(entries);
        }

    }
}
