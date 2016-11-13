// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RealtimeSearch.Search
{
    [DataContract]
    public class SearchOption
    {
        // 単語一致
        [DataMember]
        public bool IsWord { get; set; }

        // 完全一致
        [DataMember]
        public bool IsPerfect { get; set; }

        // 順番一致 (未対応)
        [DataMember]
        public bool IsOrder { get; set; }

        // フォルダーを含める
        [DataMember]
        public bool AllowFolder { get; set; }

        public SearchOption Clone()
        {
            return (SearchOption)(this.MemberwiseClone());
        }
    }

    /// <summary>
    /// 検索コア
    /// 検索フォルダのファイルをインデックス化して保存し、検索を行う
    /// </summary>
    public class SearchCore
    {
        private List<string> _roots;

        private Dictionary<string, NodeTree> _fileIndexDirectory;

        private List<string> _keys;

        public ObservableCollection<NodeContent> SearchResult { get; private set; }


        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SearchCore()
        {
            _roots = new List<string>();
            _keys = new List<string>();
            _fileIndexDirectory = new Dictionary<string, NodeTree>();
            SearchResult = new ObservableCollection<NodeContent>();
        }


        /// <summary>
        /// 検索フォルダのインデックス化
        /// </summary>
        /// <param name="roots">検索フォルダ群</param>
        public void Collect(string[] roots)
        {
            _roots.Clear();

            // 他のパスに含まれるなら除外
            foreach (var path in roots)
            {
                if (!roots.Any(p => path != p && path.StartsWith(p.TrimEnd('\\') + "\\")))
                {
                    _roots.Add(path);
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

            foreach (var root in _roots)
            {
                NodeTree sub;

                if (!_fileIndexDirectory.ContainsKey(root))
                {
                    sub = new NodeTree(root);
                }
                else
                {
                    sub = _fileIndexDirectory[root];
                }

                newDinctionary.Add(root, sub);
            }


            Node.TotalCount = 0;

            Parallel.ForEach(newDinctionary.Values, sub =>
            {
                sub.Collect();
            });

            // 再登録されなかったパスの後処理を行う
            foreach (var a in _fileIndexDirectory)
            {
                if (!newDinctionary.ContainsValue(a.Value))
                {
                    a.Value.Dispose();
                }
            }

            _fileIndexDirectory = newDinctionary;
            System.GC.Collect();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int NodeCount()
        {
            return _fileIndexDirectory.Sum(e => e.Value.NodeCount());
        }


        /// <summary>
        /// インデックス追加
        /// </summary>
        /// <param name="root">検索フォルダ</param>
        /// <param name="paths">追加パス</param>
        public Node AddPath(string root, string path)
        {
            if (!_fileIndexDirectory.ContainsKey(root))
            {
                return null;
            }

            return _fileIndexDirectory[root].AddNode(path);
        }


        /// <summary>
        /// インデックス削除
        /// </summary>
        /// <param name="root">検索フォルダ</param>
        /// <param name="path">削除パス</param>
        public Node RemovePath(string root, string path)
        {
            if (!_fileIndexDirectory.ContainsKey(root))
            {
                return null;
            }

            return _fileIndexDirectory[root].RemoveNode(path);
        }

        /// <summary>
        /// リネーム
        /// </summary>
        /// <param name="root"></param>
        /// <param name="oldFileName"></param>
        /// <param name="newFilename"></param>
        public Node RenamePath(string root, string oldFileName, string newFileName)
        {
            if (!_fileIndexDirectory.ContainsKey(root))
            {
                return null;
            }

            return _fileIndexDirectory[root].Rename(oldFileName, newFileName);
        }



        /// <summary>
        /// インデックスの情報更新
        /// </summary>
        /// <param name="root"></param>
        /// <param name="path"></param>
        public void RefleshIndex(string root, string path)
        {
            if (!_fileIndexDirectory.ContainsKey(root))
            {
                return;
            }

            _fileIndexDirectory[root].RefleshNode(path);
        }


        //
        private string GetNotCodeBlockRegexString(char c)
        {
            if ('0' <= c && c <= '9')
                return @"\D";
            //else if (('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z'))
            //    return @"\P{L}";
            else if (0x3040 <= c && c <= 0x309F)
                return @"\P{IsHiragana}";
            else if (0x30A0 <= c && c <= 0x30FF)
                return @"\P{IsKatakana}";
            else if ((0x3400 <= c && c <= 0x4DBF) || (0x4E00 <= c && c <= 0x9FFF) || (0xF900 <= c && c <= 0xFAFF))
                return @"[^\p{IsCJKUnifiedIdeographsExtensionA}\p{IsCJKUnifiedIdeographs}\p{IsCJKCompatibilityIdeographs}]";
            else if (new Regex(@"^\p{L}").IsMatch(c.ToString()))
                return @"\P{L}";
            else
                return null;
        }


        /// <summary>
        /// 検索キーを設定する
        /// </summary>
        /// <param name="source">検索キーの元</param>
        public void SetKeys(string source, SearchOption option)
        {
            //bool isFazy = false;

            // 単語検索。
            // ひらがな、カタカナは区別する
            // 開始文字が{[0-9],[a-zA-Z],\p{IsHiragana},\p{IsKatanaka},\p{IsCJKUnifiedIdeographsExtensionA}}であるならば、区切り文字はそれ以外のものとする
            //  でないなら、区切り区別はしない
            // 終端文字が..


            // 単語の順番。固定化。


            const string splitter = @"[\s]+";

            // 入力文字列を整列
            // 空白、改行文字でパーツ分け
            string s = new Regex("^" + splitter).Replace(source, "");
            s = new Regex(splitter + "$").Replace(s, "");
            var tokens = new Regex(splitter).Split(s);

            _keys.Clear();

            foreach (var token in tokens)
            {
                if (token == "") continue;

                var key = option.IsPerfect ? token : Node.ToNormalisedWord(token, !option.IsWord);

                // 正規表現記号をエスケープ
                var t = Regex.Escape(key);

                if (!option.IsPerfect)
                {
                    // (数値)部分を0*(数値)という正規表現に変換
                    t = new Regex(@"0*(\d+)").Replace(t, match => "0*" + match.Groups[1]);
                }

                if (option.IsWord)
                {
                    // 先頭文字
                    var start = GetNotCodeBlockRegexString(key.First());
                    if (start != null) t = $"(^|{start})" + t;

                    // 終端文字
                    var end = GetNotCodeBlockRegexString(key.Last());
                    if (end != null) t = t + $"({end}|$)";
                }

                _keys.Add(t);
            }
        }


        /// <summary>
        /// すべてのNodeを走査
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Node> AllNodes
        {
            get
            {
                foreach (var part in _fileIndexDirectory)
                {
                    foreach (var node in part.Value.Root.AllChildren)
                        yield return node;
                }
            }
        }

        //
        private object _lock = new object();

        //
        public void ClearSearchResult()
        {
            lock (_lock)
            {
                SearchResult.Clear();
            }
        }

        /// <summary>
        /// 検索 -- 検索結果を更新する
        /// </summary>
        /// <param name="keyword">検索キーワード</param>
        /// <param name="isSearchFolder">フォルダを検索対象に含めるフラグ</param>
        public void UpdateSearchResult(string keyword, SearchOption option)
        {
            lock (_lock)
            {
                SearchResult = new ObservableCollection<NodeContent>(Search(keyword, AllNodes, option).Select(e => e.Content));
            }
        }

        /// <summary>
        /// 検索
        /// </summary>
        /// <param name="keyword">検索キーワード</param>
        /// <param name="entries">検索対象</param>
        /// <param name="isSearchFolder">フォルダを検索対象に含めるフラグ</param>
        /// <returns></returns>
        public IEnumerable<Node> Search(string keyword, IEnumerable<Node> entries, SearchOption option)
        {
            // pushpin保存
            var pushpins = entries.Where(f => f.Content.IsPushPin);

            // キーワード登録
            SetKeys(keyword, option);
            if (_keys == null || _keys[0] == "^$")
            {
                return pushpins;
            }


            // キーワードによる絞込
            foreach (var key in _keys)
            {
                var regex = new Regex(key, RegexOptions.Compiled);
                if (option.IsPerfect)
                {
                    entries = entries.Where(f => regex.Match(f.Name).Success);
                }
                else if (option.IsWord)
                {
                    entries = entries.Where(f => regex.Match(f.NormalizedUnitWord).Success);
                }
                else
                {
                    entries = entries.Where(f => regex.Match(f.NormalizedFazyWord).Success);
                }
            }

            // ディレクトリ除外
            if (!option.AllowFolder)
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
