using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NeeLaboratory.Collections
{
    public class Node<T>
    {
        private readonly Lock _lock = new();

        public Node(string name)
        {
            Name = name.TrimEnd('\\');

            // NOTE: ネットワークパス用に先頭の区切り文字を許容する
            if (Name.TrimStart('\\').Contains("\\")) throw new ArgumentException("It contains a delimiter: '\\'");
        }

        public string Name { get; set; }

        public Node<T>? Parent { get; set; }

        public List<Node<T>>? Children { get; set; }

        public string FullName => Path.Combine(Parent?.FullName ?? "", Name);

        public T? Content { get; set; }


        public bool HasParent(Node<T> node)
        {
            return Parent != null && (Parent == node || Parent.HasParent(node));
        }

        /// <summary>
        /// 子ノード全削除
        /// </summary>
        public void ClearChildren()
        {
            lock (_lock)
            {
                if (Children is null) return;
                foreach (var child in Children)
                {
                    child.Parent = null;
                }
                Children = null;
            }
        }

        /// <summary>
        /// ノード検索
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Node<T>? FindChild(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException();

            if (Children is null) return null;
            return Children.FirstOrDefault(e => e.Name == name);
        }

        /// <summary>
        /// ノード追加
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public Node<T> AddChild(Node<T> node)
        {
            lock (_lock)
            {
                if (node.Parent is not null)
                {
                    node.Parent.RemoveChild(node);
                    Debug.Assert(node.Parent is null);
                }

                if (Children is null)
                {
                    Children = new List<Node<T>>();
                }

                Children.Add(node);
                node.Parent = this;
                return node;
            }
        }

        /// <summary>
        /// ノード追加
        /// </summary>
        /// <param name="name">名前</param>
        /// <returns>追加されたノード</returns>
        /// <exception cref="ArgumentException"><paramref name="name"/> が空</exception>
        /// <exception cref="InvalidOperationException">既に登録されている</exception>
        public Node<T> AddChild(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException($"Argument is empty: {nameof(name)}");
            if (FindChild(name) != null) throw new ArgumentException($"Already exists: {nameof(name)}");

            return AddChild(new Node<T>(name));
        }

        /// <summary>
        /// ノード削除
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public Node<T>? RemoveChild(Node<T> node)
        {
            if (node.Parent != this) return null;

            lock (_lock)
            {
                node.Parent = null;
                Children?.Remove(node);
                return node;
            }
        }

        /// <summary>
        /// ノード削除
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Node<T>? RemoveChild(string name)
        {
            var node = FindChild(name);
            if (node is null) return null;

            return RemoveChild(node);
        }

        /// <summary>
        /// ノード削除
        /// </summary>
        /// <param name="children"></param>
        public void RemoveChildren(IEnumerable<Node<T>> children)
        {
            foreach (var child in children)
            {
                RemoveChild(child);
            }
        }

        /// <summary>
        /// 自身をノードから削除
        /// </summary>
        /// <returns></returns>
        public Node<T>? RemoveSelf()
        {
            if (this.Parent is null) return null;
            return this.Parent.RemoveChild(this);
        }

        /// <summary>
        /// 子ノードのクローンを作成
        /// </summary>
        /// <returns></returns>
        public List<Node<T>> CloneChildren()
        {
            if (Children is null) return [];

            lock (_lock)
            {
                var clone = new List<Node<T>>(Children);
                Debug.Assert(clone != Children);
                return clone;
            }
        }

        /// <summary>
        /// ノードを再帰的に走査する。自分は含まない。
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Node<T>> WalkChildren()
        {
            if (Children is not null)
            {
                foreach (var child in Children)
                {
                    foreach (var node in child.Walk())
                    {
                        yield return node;
                    }
                }
            }
        }

        /// <summary>
        /// ノードを再帰的に走査する。自分を含む。
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Node<T>> Walk()
        {
            yield return this;

            if (Children is not null)
            {
                foreach (var child in Children)
                {
                    foreach (var node in child.Walk())
                    {
                        yield return node;
                    }
                }
            }
        }

        /// <summary>
        /// 深さつき Walk
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        public IEnumerable<(Node<T> Node, int Depth)> WalkChildrenWithDepth(int depth = 0)
        {
            if (Children is not null)
            {
                foreach (var child in Children)
                {
                    foreach (var node in child.WalkWithDepth(depth + 1))
                    {
                        yield return node;
                    }
                }
            }
        }

        /// <summary>
        /// 深さつき Walk
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        public IEnumerable<(Node<T> Node, int Depth)> WalkWithDepth(int depth = 0)
        {
            yield return (this, depth);

            if (Children is not null)
            {
                foreach (var child in Children)
                {
                    foreach (var node in child.WalkWithDepth(depth + 1))
                    {
                        yield return node;
                    }
                }
            }
        }

        public override string ToString()
        {
            return FullName;
        }


        /// <summary>
        /// 開発用：ツリー出力
        /// </summary>
        /// <param name="level"></param>
        [Conditional("DEBUG")]
        public void Dump(int level = 0)
        {
            var text = new string(' ', level * 4) + Name;
            Debug.WriteLine(text);
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    child.Dump(level + 1);
                }
            }

            ////Logger.Trace($"{Path}:({AllNodes.Count()})");
        }

    }
}