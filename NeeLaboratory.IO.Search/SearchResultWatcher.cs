using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeeLaboratory.IO.Search
{
    public class SearchResultWatcher : IDisposable
    {
        private SearchEngine _engine;
        private SearchResult _result;

        public SearchResultWatcher(SearchEngine engine, SearchResult result)
        {
            _engine = engine;
            _result = result;
        }

        public void Start()
        {
            _engine.Core.NodeChanged += Core_NodeChanged;
        }

        public void Stop()
        {
            _engine.Core.NodeChanged -= Core_NodeChanged;
        }

        private void Core_NodeChanged(object sender, NodeChangedEventArgs e)
        {
            var node = e.Node;
            //Node node = _searchCore.AddPath(root, path);
            if (node == null) return;

            if (e.Action == NodeChangedAction.Add)
            {
                var items = _engine.Core.Search(_result.Keyword, node.AllNodes, _result.SearchOption);
                foreach (var item in items)
                {
                    Debug.WriteLine($"Add: {item.Name}");
                    _result.Items.Add(item.Content);
                }
            }
            else if (e.Action == NodeChangedAction.Remove)
            {
                var items = _result.Items.Where(item => item.IsRemoved).ToList();
                foreach (var item in items)
                {
                    Debug.WriteLine($"Remove: {item.Name}");
                    _result.Items.Remove(item);
                }
            }
            else if (e.Action == NodeChangedAction.Rename)
            {
                if (!_result.Items.Contains(node.Content))
                {
                    var items = _engine.Core.Search(_result.Keyword, new List<Node>() { node }, _result.SearchOption);
                    foreach (var item in items)
                    {
                        Debug.WriteLine($"Add: {item.Name}");
                        _result.Items.Add(item.Content);
                    }
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                    Stop();
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~SearchResultWatcher() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
