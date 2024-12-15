//#define LOCAL_DEBUG
using NeeLaboratory.ComponentModel;
using NeeLaboratory.Generators;
using NeeLaboratory.IO;
using NeeLaboratory.IO.Search;
using NeeLaboratory.Threading;
using NeeLaboratory.Threading.Jobs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace NeeLaboratory.IO.Search.Files
{
    [NotifyPropertyChanged]
    public partial class FileSearchEngine : INotifyPropertyChanged, IDisposable
    {
        // TODO: cache を無効化
        public static Searcher DefaultSearcher = new Searcher(new FileSearchContext(SearchValueCacheFactory.CreateWithoutCache()));

        private readonly ISearchContext _context;
        private readonly Searcher _searcher;
        private readonly FileForest _tree;
        private CancellationTokenSource? _indexCancellationTokenSource;
        private CancellationTokenSource? _searchCancellationTokenSource;
        private bool _disposedValue;
        private readonly SlimJobEngine _jobEngine;
        private AsyncLock _collectLock = new();
        private bool _isCollectBusy;
        private bool _isSearchBusy;


        public FileSearchEngine(ISearchContext context)
        {
            _context = context;
            //Path = path;
            //IncludeSubdirectories = includeSubdirectories;
            //AllowHidden = allowHidden;

            _tree = new FileForest();
            _tree.CollectBusyChanged += Tree_CollectBusyChanged;
            _searcher = new Searcher(new FileSearchContext(SearchValueCacheFactory.Create()));
            UpdateSearchProperties();

            _jobEngine = new SlimJobEngine(nameof(FileSearchEngine));

            _context.PropertyChanged += SearchContext_PropertyChanged;
        }


        [Subscribable]
        public event PropertyChangedEventHandler? PropertyChanged;


        public bool IsBusy { get; private set; }

        public FileForest Tree => _tree;

        public bool IsCollectBusy
        {
            get { return _isCollectBusy; }
            set { SetProperty(ref _isCollectBusy, value); }
        }

        public bool IsSearchBusy
        {
            get { return _isSearchBusy; }
            set { SetProperty(ref _isSearchBusy, value); }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _indexCancellationTokenSource?.Cancel();
                    _indexCancellationTokenSource?.Dispose();
                    _indexCancellationTokenSource = null;

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

        private void Tree_CollectBusyChanged(object? sender, bool e)
        {
            IsCollectBusy = e;
        }

        private IDisposable CreateSearchSection()
        {
            IsSearchBusy = true;
            return new AnonymousDisposable(() => IsSearchBusy = false);
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

        public void AddSearchAreas(params FileArea[] areas)
        {
            _tree.AddSearchAreas(areas);
            _ = IndexAsync();
        }

        public void SetSearchAreas(IEnumerable<FileArea> areas, FileForestMemento? memento = null)
        {
            _tree.SetSearchAreas(areas, memento);
            _ = IndexAsync();
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


        public async Task IndexAsync()
        {
            ThrowIfDisposed();

            _indexCancellationTokenSource?.Cancel();
            _indexCancellationTokenSource?.Dispose();
            _indexCancellationTokenSource = new CancellationTokenSource();

            var job = _jobEngine.InvokeAsync(() => IndexInner(_indexCancellationTokenSource.Token));
            await job;
            if (job.Exception is not null)
            {
                throw job.Exception;
            }
        }

        private void IndexInner(CancellationToken token)
        {
            _tree.Wait(token);
            _tree.Initialize(token);
        }

        public async Task<SearchResult<FileContent>> SearchAsync(string keyword, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource?.Dispose();
            _searchCancellationTokenSource = new CancellationTokenSource();

            try
            {
                IsBusy = true;
                var job = _jobEngine.InvokeAsync(() => SearchInner(keyword, token));
                await job;
                if (job.Exception is not null)
                {
                    throw job.Exception;
                }
                return job.Result ?? new SearchResult<FileContent>(keyword);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private SearchResult<FileContent> SearchInner(string keyword, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            using var section = CreateSearchSection();

            try
            {
                using (_tree.Lock(token))
                {
                    var entries = _tree.CollectFileContents();
                    var items = _searcher.Search(keyword, entries, token).ToList();
                    return new SearchResult<FileContent>(keyword, items.Cast<FileContent>());
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new SearchResult<FileContent>(keyword, null, ex);
            }
        }


        private async Task<SearchResult<FileContent>> SearchInnerAsync(string keyword, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            using var section = CreateSearchSection();

            try
            {
                using (await _tree.LockAsync(token))
                {
                    // 検索
                    var entries = _tree.CollectFileContents();
                    var items = await Task.Run(() => _searcher.Search(keyword, entries, token).ToList());
                    return new SearchResult<FileContent>(keyword, items.Cast<FileContent>());
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new SearchResult<FileContent>(keyword, null, ex);
            }
        }

        public List<FileContent> Search(string keyword, IEnumerable<FileContent> entries, CancellationToken token)
        {
            // NOTE: 非依存なので独立して処理可能
            return _searcher.Search(keyword, entries, token).Cast<FileContent>().ToList();
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
                await _tree.WaitAsync(CancellationToken.None);
                await _tree.InitializeAsync(token);
                using (await _tree.LockAsync(token))
                {
                    // 検索
                    using var section = CreateSearchSection();
                    var entries = _tree.CollectFileContents();
                    var units = keywords.Select(e => new MultiSearchUnit(e)).ToList();
                    Parallel.ForEach(units, unit =>
                    {
                        try
                        {
                            var items = _searcher.Search(unit.Keyword, entries, tokenSource.Token).ToList();
                            unit.Result = new FileSearchResultWatcher(this, new SearchResult<FileContent>(unit.Keyword, items.Cast<FileContent>()));
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            unit.Result = new FileSearchResultWatcher(this, new SearchResult<FileContent>(unit.Keyword, null, ex));
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
                return keywords.Select(e => new FileSearchResultWatcher(this, new SearchResult<FileContent>(e, null, ex))).ToList();
            }
            finally
            {
                tokenSource.Dispose();
                IsBusy = false;
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
