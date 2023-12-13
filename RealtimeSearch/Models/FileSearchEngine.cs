//#define LOCAL_DEBUG
using NeeLaboratory.IO;
using NeeLaboratory.IO.Search;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace NeeLaboratory.RealtimeSearch
{

    public class FileSearchEngine : IDisposable
    {
        // TODO: cache を無効化
        public static Searcher DefaultSearcher = new Searcher(new FileSearchContext(SearchValueCacheFactory.CreateWithoutCache()));

        private readonly AppConfig _config;
        private readonly Searcher _searcher;
        private readonly FileItemForest _tree;
        private CancellationTokenSource? _searchCancellationTokenSource;
        private bool _disposedValue;


        public FileSearchEngine(AppConfig config, bool includeSubdirectories, bool allowHidden)
        {
            _config = config;
            //Path = path;
            IncludeSubdirectories = includeSubdirectories;
            AllowHidden = allowHidden;

            _tree = new FileItemForest(IOExtensions.CreateEnumerationOptions(includeSubdirectories, allowHidden));
            _searcher = new Searcher(new FileSearchContext(SearchValueCacheFactory.Create()));
            UpdateSearchProperties();

            _config.PropertyChanged += AppConfig_PropertyChanged;
        }

        private void AppConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(AppConfig.AllowFolder):
                    UpdateSearchProperties();
                    break;
            }
        }

        public bool IncludeSubdirectories { get; }

        public bool AllowHidden { get; }

        public bool IsBusy { get; private set; }


        public IFileItemTree Tree => _tree;

        public SearchCommandEngineState State { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _searchCancellationTokenSource?.Cancel();
                    _searchCancellationTokenSource?.Dispose();
                    _searchCancellationTokenSource = null;

                    _tree.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected void ThrowIfDisposed()
        {
            if (_disposedValue) throw new ObjectDisposedException(GetType().FullName);
        }


        public void SetSearchAreas(IEnumerable<string> paths)
        {
            _tree.UpdateTrees(paths);
        }


        private void UpdateSearchProperties()
        {
            // allow folder
            _searcher.PreKeys = _config.AllowFolder ? new() : new() { new SearchKey(SearchConjunction.And, ExtraSearchPropertyProfiles.IsDirectory, null, SearchFilterProfiles.Equal, "false") };

            // pushpin
            _searcher.PostKeys = new() { new SearchKey(SearchConjunction.PreOr, ExtraSearchPropertyProfiles.IsPinned, null, SearchFilterProfiles.Equal, "true") };
        }


        public IEnumerable<SearchKey> Analyze(string keyword)
        {
            return _searcher.Analyze(keyword);
        }

        public void CancelSearch()
        {
            _searchCancellationTokenSource?.Cancel();
        }

        public async Task<FileSearchResultWatcher> SearchAsync(string keyword, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource?.Dispose();
            _searchCancellationTokenSource = new CancellationTokenSource();

            IsBusy = true;
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, _searchCancellationTokenSource.Token);

            try
            {
                await _tree.InitializeAsync(token);

                using (await _tree.LockAsync(token))
                {
                    State = SearchCommandEngineState.Collect;
                    var entries = _tree.CollectFileItems();

                    // 検索
                    State = SearchCommandEngineState.Search;
                    var items = await Task.Run(() => _searcher.Search(keyword, entries, tokenSource.Token).ToList());

                    // 監視開始
                    var watcher = new FileSearchResultWatcher(this, new SearchResult<FileItem>(keyword, items.Cast<FileItem>()));
                    return watcher;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var watcher = new FileSearchResultWatcher(this, new SearchResult<FileItem>(keyword, null, ex));
                return watcher;
            }
            finally
            {
                tokenSource.Dispose();
                IsBusy = false;
                State = SearchCommandEngineState.Idle;
            }
        }


        public async Task<List<FileItem>> SearchAsync(string keyword, IEnumerable<FileItem> entries, CancellationToken token)
        {
            return await Task.Run(() => _searcher.Search(keyword, entries, token).Cast<FileItem>().ToList());
        }
    }

}
