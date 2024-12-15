using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace NeeLaboratory.Collections
{
    /// <summary>
    /// パス木構造のみ管理
    /// </summary>
    public class NodeTree<T>
    {
        private readonly Node<T> _root = new Node<T>("");
        private Node<T> _trunk;

        public NodeTree(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            var names = SplitPath(path);

            var node = _root;
            foreach (var name in names)
            {
                node = node.AddChild(name);
            }
            _trunk = node;
        }


        public Node<T> Root => _root;

        public Node<T> Trunk => _trunk;


        public void SetTrunk(Node<T> node)
        {
            Debug.Assert(node.Name == Trunk.Name);

            var parent = Trunk.Parent;
            Debug.Assert(parent is not null);
            parent.RemoveChild(Trunk);
            parent.AddChild(node);
            _trunk = node;
        }

        private static string[] SplitPath(string path)
        {
            var tokens = path.Split('\\', StringSplitOptions.RemoveEmptyEntries);

            // ネットワークパス用に先頭の区切り文字を戻す
            if (tokens.Any() && path[0] == '\\')
            {
                tokens[0] = string.Concat(path.TakeWhile(e => e == '\\').Concat(tokens[0]));
            }

            return tokens;
        }


        public Node<T>? Find(string path)
        {
            return Find(path, true);
        }

        public Node<T>? Find(string path, bool inTrunk)
        {
            var node = _root;
            var tokens = SplitPath(path);

            foreach (var token in tokens)
            {
                node = node.Children?.FirstOrDefault(e => e.Name == token);
                if (node is null) return null;
            }

            if (inTrunk && !node.HasParent(_trunk)) return null;

            return node;
        }

        public Node<T>? Add(string path)
        {
            var node = _root;
            var names = SplitPath(path);
            if (!names.Any()) throw new ArgumentException();
            var isNew = false;

            foreach (var name in names)
            {
                var next = node.FindChild(name);
                if (next is null)
                {
                    if (node == _trunk || node.HasParent(_trunk))
                    {
                        next = node.AddChild(name);
                        isNew = true;
                    }
                    else
                    {
                        return null;
                    }
                }
                node = next;
            }

            Debug.Assert(!isNew || node.FullName == path);
            return isNew ? node : null;
        }

        public Node<T>? Remove(string path)
        {
            var node = Find(path);
            if (node is null) return null;

            node.Parent?.RemoveChild(node);
            node.Parent = null;
            return node;
        }

        public Node<T>? Rename(string path, string name)
        {
            var node = Find(path);
            if (node is null) return null;

            node.Name = name;
            return node;
        }

        public IEnumerable<Node<T>> WalkAll()
        {
            return _trunk.WalkChildren();
        }


        [Conditional("DEBUG")]
        public void Dump()
        {
            Debug.WriteLine($"NodeTree: {_root.FullName}");
            foreach (var node in _root.WalkChildren())
            {
                Debug.WriteLine(node.FullName);
            }
            Debug.WriteLine(".");
        }

        [Conditional("DEBUG")]
        public void Validate()
        {
            var sw = Stopwatch.StartNew();

            Debug.Assert(_root.Parent is null);
            Debug.Assert(_root.Name is "");

            foreach (var node in _root.WalkChildren())
            {
                Debug.Assert(node.Parent is not null);
                Debug.Assert(node.Parent.Children is not null);
                Debug.Assert(node.Parent.Children.Contains(node));
                Debug.Assert(!string.IsNullOrEmpty(node.Name));
                Debug.Assert(node.Children is null || !node.Children.GroupBy(i => i).SelectMany(g => g.Skip(1)).Any());
            }

            sw.Stop();
            Debug.WriteLine($"Validate {_trunk.FullName}: {sw.ElapsedMilliseconds} ms");
        }

    }
}