//#define LOCAL_DEBUG
using NeeLaboratory.IO;
using NeeLaboratory.IO.Search;
using NeeLaboratory.IO.Search.FileNode;
using NeeLaboratory.IO.Search.FileSearch;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;


namespace NeeLaboratory.RealtimeSearch
{

    public class FileSearchEngine : IDisposable
    {
        // TODO: cache を無効化
        public static Searcher DefaultSearcher = new Searcher(new FileSearchContext(SearchValueCacheFactory.CreateWithoutCache()));

        private readonly ISearchContext _context;
        private readonly Searcher _searcher;
        private readonly FileItemForest _tree;
        private CancellationTokenSource? _searchCancellationTokenSource;
        private bool _disposedValue;


        public FileSearchEngine(ISearchContext context)
        {
            _context = context;
            //Path = path;
            //IncludeSubdirectories = includeSubdirectories;
            //AllowHidden = allowHidden;

            _tree = new FileItemForest();
            _searcher = new Searcher(new FileSearchContext(SearchValueCacheFactory.Create()));
            UpdateSearchProperties();

            _context.PropertyChanged += SearchContext_PropertyChanged;
        }

        private void SearchContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ISearchContext.AllowFolder):
                    UpdateSearchProperties();
                    break;
            }
        }

        //public bool IncludeSubdirectories { get; }

        //public bool AllowHidden { get; }

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


        public void AddSearchAreas(params NodeArea[] areas)
        {
            _tree.AddSearchAreas(areas);
        }

        public void SetSearchAreas(IEnumerable<NodeArea> areas)
        {
            _tree.SetSearchAreas(areas);
        }


        private void UpdateSearchProperties()
        {
            // allow folder
            _searcher.PreKeys = _context.AllowFolder ? new() : new() { new SearchKey(SearchConjunction.And, ExtraSearchPropertyProfiles.IsDirectory, null, SearchFilterProfiles.Equal, "false") };

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
                State = SearchCommandEngineState.Collect;

                await _tree.WaitAsync(CancellationToken.None);

                await _tree.InitializeAsync(token);

                using (await _tree.LockAsync(token))
                {
                    // 検索
                    State = SearchCommandEngineState.Search;
                    var entries = _tree.CollectFileItems();
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


        #region Multi-Search

        /// <summary>
        /// マルチ検索
        /// </summary>

        public async Task<List<FileSearchResultWatcher>> MultiSearchAsync(IEnumerable<string> keywords, CancellationToken token)
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
                // 収集
                State = SearchCommandEngineState.Collect;
                await _tree.WaitAsync(CancellationToken.None);
                await _tree.InitializeAsync(token);
                using (await _tree.LockAsync(token))
                {
                    // 検索
                    State = SearchCommandEngineState.Search;
                    var entries = _tree.CollectFileItems();
                    var units = keywords.Select(e => new MultiSearchUnit(e)).ToList();
                    Parallel.ForEach(units, unit =>
                    {
                        try
                        {
                            var items = _searcher.Search(unit.Keyword, entries, tokenSource.Token).ToList();
                            unit.Result = new FileSearchResultWatcher(this, new SearchResult<FileItem>(unit.Keyword, items.Cast<FileItem>()));
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            unit.Result = new FileSearchResultWatcher(this, new SearchResult<FileItem>(unit.Keyword, null, ex));
                        }
                    });

                    return units
                        .Select(e => e.Result ?? throw new InvalidOperationException("Result must not be null"))
                        .ToList();
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return keywords.Select(e => new FileSearchResultWatcher(this, new SearchResult<FileItem>(e, null, ex))).ToList();
            }
            finally
            {
                tokenSource.Dispose();
                IsBusy = false;
                State = SearchCommandEngineState.Idle;
            }
        }

        private class MultiSearchUnit
        {
            public MultiSearchUnit(string keyword)
            {
                Keyword = keyword;
            }

            public string Keyword { get; set; }
            public FileSearchResultWatcher? Result { get; set; }
        }


        #endregion


    }

}
