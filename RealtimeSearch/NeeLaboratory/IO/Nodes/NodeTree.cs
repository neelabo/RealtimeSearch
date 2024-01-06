using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace NeeLaboratory.IO.Nodes
{
    /// <summary>
    /// パス木構造のみ管理
    /// </summary>
    public class NodeTree
    {
        private readonly Node _root = new Node("");
        private readonly Node _trunk;

        public NodeTree(string path)
        {
            var node = _root;
            var names = SplitPath(path);

            foreach (var name in names)
            {
                node = node.AddChild(name);
            }
            _trunk = node;
        }


        public Node Root => _root;

        public Node Trunk => _trunk;


        private static IEnumerable<string> SplitPath(string path)
        {
            var tokens = path.Split('\\', StringSplitOptions.RemoveEmptyEntries);

            // ネットワークパス用に先頭の区切り文字を戻す
            if (tokens.Any())
            {
                foreach (var c in path)
                {
                    if (c != '\\') break;
                    tokens[0] = "\\" + tokens[0];
                }
            }

            return tokens;
        }

        public Node? Find(string path)
        {
            return Find(path, true);
        }

        public Node? Find(string path, bool inTrunk)
        {
            var node = _root;
            var tokens = path.Split('\\', StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                node = node.Children?.FirstOrDefault(e => e.Name == token);
                if (node is null) return null;
            }

            if (inTrunk && !node.HasParent(_trunk)) return null;

            return node;
        }

        public Node? Add(string path)
        {
            var node = _root;
            var names = path.Split('\\', StringSplitOptions.RemoveEmptyEntries);
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

        public Node? Remove(string path)
        {
            var node = Find(path);
            if (node is null) return null;

            node.Parent?.RemoveChild(node);
            node.Parent = null;
            return node;
        }


        public Node? Rename(string path, string name)
        {
            var node = Find(path);
            if (node is null) return null;

            node.Name = name;
            return node;
        }

        public IEnumerable<Node> WalkAll()
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
        }
    }

}