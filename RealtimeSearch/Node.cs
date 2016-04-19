﻿// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
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
    /// <summary>
    /// Node
    /// </summary>
    public class Node
    {
        private string _Name;
        public string Name
        {
            get { return _Name; }
            private set
            {
                _Name = value;
                NormalizedWord = ToNormalisedWord(_Name);
            }
        }

        //
        public void Rename(string name)
        {
            Name = name;

            foreach (var node in AllNodes())
            {
                node.RefleshPath();
            }
        }

        private void RefleshPath()
        {
            if (Content != null)
            {
                Content.Path = Path;
                Content.Reflesh();
            }
        }

        public Node Parent { get; private set; }
        public List<Node> Children { get; set; }

        public string Path => Parent == null ? Name : System.IO.Path.Combine(Parent.Path, Name);

        // ディレクトリ？
        public bool IsDirectory => Children != null;

        // 検索用正規化ファイル名
        public string NormalizedWord { get; private set; }

        // コンテンツ
        public NodeContent Content;


        // コンストラクタ
        public Node(string name, Node parent)
        {
            Name = name;
            Parent = parent;
            Content = new NodeContent(Path);
        }

        // ファイル情報更新
        public void Reflesh()
        {
            Content.Reflesh();

            if (IsDirectory)
            {
                foreach (var child in Children)
                {
                    child.Reflesh();
                }
            }
        }


        // 正規化された文字列に変換する
        public static string ToNormalisedWord(string src)
        {
            string s = src.Normalize(NormalizationForm.FormKC); // 正規化
            s = s.Replace(" ", ""); // 空白を削除する

            s = s.ToUpper(); // アルファベットを大文字にする
            s = Microsoft.VisualBasic.Strings.StrConv(s, Microsoft.VisualBasic.VbStrConv.Katakana); // ひらがなをカタカナにする
            s = s.Replace("ー", "-"); // 長音をハイフンにする 

            return s;
        }


        // 表示文字列
        public override string ToString()
        {
            return Name;
        }

        //
        public static Node CreateTree(string name, Node parent, bool isDirectoryMaybe)
        {
            Node node = new Node(name, parent);

            if (isDirectoryMaybe && Directory.Exists(node.Path))
            {
                try
                {
                    var directories = Directory.GetDirectories(node.Path).Select(s => System.IO.Path.GetFileName(s)).ToList();
                    directories.Sort(Win32Api.StrCmpLogicalW);

                    var files = Directory.GetFiles(node.Path).Select(s => System.IO.Path.GetFileName(s)).ToList();
                    files.Sort(Win32Api.StrCmpLogicalW);

#if true
                    var directoryNodes = new Node[directories.Count];
                    Parallel.ForEach(directories, (s, state, index) =>
                    {
                        directoryNodes[(int)index] = CreateTree(s, node, true);
                    });

                    var fileNodes = files.Select(s => CreateTree(s, node, false));

                    node.Children = directoryNodes.Concat(fileNodes).ToList();
#else
                    node.Children = directories.Concat(files).Select(s => CreateTree(s, node)).ToList();
#endif
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    node.Children = new List<Node>();
                }
            }

            return node;
        }

        static readonly char[] splitter = new char[] { '\\' };

        //
        private Node Scanning(string path, bool isCreate)
        {
            if (path == Name) return this;

            if (!IsDirectory) return null;
            if (!path.StartsWith(Name + '\\')) return null;
            var childPath = path.Substring(Name.Length + 1);

            foreach (var child in Children)
            {
                var node = child.Scanning(childPath, isCreate);
                if (node != null) return node;
            }

            if (!isCreate) return null;

            // 作成
            var tokens = childPath.Split(splitter, 2);
            var childNode = CreateTree(tokens[0], this, true);
            this.Children.Add(childNode);
            childNode.Content.IsAdded = true;
            return childNode;
        }

        //
        public Node Search(string path)
        {
            return Scanning(path, false);
        }

        //
        public Node Add(string path)
        {
            var node = Scanning(path, true);
            if (node != null && node.Content.IsAdded)
            {
                node.Content.IsAdded = false; // 追加フラグをOFFにしておく
                return node;
            }
            else
            {
                return null;
            }
        }

        //
        public Node Remove(string path)
        {
            var node = Scanning(path, false);
            if (node == null) return null;

            node.Parent?.Children.Remove(node);
            node.Parent = null;

            foreach (var n in node.AllNodes())
            {
                n.Content.IsRemoved = true;
            }

            return node;
        }


        /// <summary>
        /// すべてのNodeを走査。自身は含まない
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Node> AllChildren()
        {
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    yield return child;
                    foreach (var node in child.AllChildren())
                    {
                        yield return node;
                    }
                }
            }
        }

        /// <summary>
        /// すべてのNodeを走査。自身を含む
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Node> AllNodes()
        {
            yield return this;
            foreach (var child in AllChildren())
            {
                yield return child;
            }
        }

        //
        [Conditional("DEBUG")]
        public void Dump(int level = 0)
        {
#if false
            var text = new string(' ', level * 4) + Name + (IsDirectory ? "\\" : "");
            Debug.WriteLine(text);
            if (IsDirectory)
            {
                foreach (var child in Children)
                {
                    child.Dump(level + 1);
                }
            }
#endif
            //Debug.WriteLine($"{Path}:({AllNodes().Count()})");
        }
    }

}
