//#define LOCAL_DEBUG
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
        private readonly FileItemForest _tree;
        private CancellationTokenSource? _indexCancellationTokenSource;
        private CancellationTokenSource? _searchCancellationTokenSource;
        private bool _disposedValue;

        private readonly SlimJobEngine _jobEngine;
        private AsyncLock _collectLock = new();
        private SearchCommandEngineState _state;


        public FileSearchEngine(ISearchContext context)
        {
            _context = context;
            //Path = path;
            //IncludeSubdirectories = includeSubdirectories;
            //AllowHidden = allowHidden;

            _tree = new FileItemForest();
            _searcher = new Searcher(new FileSearchContext(SearchValueCacheFactory.Create()));
            UpdateSearchProperties();

            _jobEngine = new SlimJobEngine(nameof(FileSearchEngine));

            _context.PropertyChanged += SearchContext_PropertyChanged;
        }




        [Subscribable]
        public event PropertyChangedEventHandler? PropertyChanged;

        [Subscribable]
        public event EventHandler<SearchCommandEngineState>? StateChanged;


        public bool IsBusy { get; private set; }


        public IFileItemTree Tree => _tree;

        public SearchCommandEngineState State
        {
            get { return _state; }
            private set
            {
                if (SetProperty(ref _state, value))
                {
                    StateChanged?.Invoke(this, _state);
                }
            }
        }


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
            _ = IndexAsync(CancellationToken.None);
        }

        public void SetSearchAreas(IEnumerable<FileArea> areas)
        {
            _tree.SetSearchAreas(areas);
            _ = IndexAsync(CancellationToken.None);
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


        public async Task IndexAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            _indexCancellationTokenSource?.Cancel();
            _indexCancellationTokenSource?.Dispose();
            _indexCancellationTokenSource = new CancellationTokenSource();

            //var job = new FileIndexJob(this);
            //_jobEngine.Enqueue(job);

            var job = _jobEngine.InvokeAsync(() => IndexInner(token));
            await job;
            if (job.Exception is not null)
            {
                throw job.Exception;
            }
        }

        private void IndexInner(CancellationToken token)
        {
            State = SearchCommandEngineState.Collect;
            try
            {
                //var sw = Stopwatch.StartNew();
                _tree.Wait(token);
                _tree.Initialize(token);
                //sw.Stop();
                //Debug.WriteLine($"ForestInitialize: {sw.ElapsedMilliseconds} ms");
            }
            finally
            {
                State = SearchCommandEngineState.Idle;
            }
        }

        public async Task IndexInnerAsync(CancellationToken token)
        {
            State = SearchCommandEngineState.Collect;
            try
            {
                await _tree.WaitAsync(token);
                await _tree.InitializeAsync(token);
            }
            finally
            {
                State = SearchCommandEngineState.Idle;
            }
        }


        public async Task<SearchResult<FileItem>> SearchAsync(string keyword, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource?.Dispose();
            _searchCancellationTokenSource = new CancellationTokenSource();

            try
            {
                IsBusy = true;
                //var job = new FileSearchJob(this, keyword);
                //_jobEngine.Enqueue(job);
                //await job.WaitAsync(_searchCancellationTokenSource.Token);
                //return job.Result;

                var job =  _jobEngine.InvokeAsync(() => SearchInner(keyword, token));
                await job;
                if (job.Exception is not null)
                {
                    throw job.Exception;
                }
                return job.Result ?? new SearchResult<FileItem>(keyword);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private SearchResult<FileItem> SearchInner(string keyword, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            State = SearchCommandEngineState.Search;
            try
            {
                using (_tree.Lock(token))
                {
                    var entries = _tree.CollectFileItems();
                   var items = _searcher.Search(keyword, entries, token).ToList();
                    return new SearchResult<FileItem>(keyword, items.Cast<FileItem>());
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new SearchResult<FileItem>(keyword, null, ex);
            }
            finally
            {
                State = SearchCommandEngineState.Idle;
            }
        }


        private async Task<SearchResult<FileItem>> SearchInnerAsync(string keyword, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            State = SearchCommandEngineState.Search;
            try
            {
                using (await _tree.LockAsync(token))
                {
                    // 検索
                    var entries = _tree.CollectFileItems();
                    var items = await Task.Run(() => _searcher.Search(keyword, entries, token).ToList());
                    return new SearchResult<FileItem>(keyword, items.Cast<FileItem>());
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new SearchResult<FileItem>(keyword, null, ex);
            }
            finally
            {
                State = SearchCommandEngineState.Idle;
            }
        }

        public List<FileItem> Search(string keyword, IEnumerable<FileItem> entries, CancellationToken token)
        {
            // NOTE: 非依存なので独立して処理可能
            return _searcher.Search(keyword, entries, token).Cast<FileItem>().ToList();
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

#if false
        private class FileIndexJob : JobBase
        {
            private readonly FileSearchEngine _engine;

            public FileIndexJob(FileSearchEngine engine)
            {
                _engine = engine;
            }

            protected override async Task ExecuteAsync(CancellationToken token)
            {
                await _engine.IndexInnerAsync(token);
            }
        }

        private class FileSearchJob : JobBase
        {
            private readonly FileSearchEngine _engine;
            private readonly string _keyword;

            public FileSearchJob(FileSearchEngine engine, string keyword)
            {
                _engine = engine;
                _keyword = keyword;
                Result = new SearchResult<FileItem>(_keyword, null);
            }

            public SearchResult<FileItem> Result { get; set; }

            protected override async Task ExecuteAsync(CancellationToken token)
            {
                try
                {
                    Result = await _engine.SearchInnerAsync(_keyword, token);
                }
                catch (Exception ex)
                {
                    Result = new SearchResult<FileItem>(_keyword, null, ex);
                }
            }
        }
#endif

        public void ReserveRename(string src, string dst)
        {
            _tree.ReserveRename(src, dst);
        }
    }




}
