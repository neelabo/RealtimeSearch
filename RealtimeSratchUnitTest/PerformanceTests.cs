#nullable enable
using System;
using Xunit;
using Xunit.Abstractions;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using NeeLaboratory.IO.Search.Files;

namespace RealtimeSearchUnitTest
{
    public class PerformanceTests
    {
        private readonly ITestOutputHelper _output;

        public PerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestPerformance()
        {
            var benchmark = new SearchBenchmark();
            var sw = new Stopwatch();

            for (int i = 0; i < 10; i++)
            {
                sw.Restart();
                var result = await benchmark.SearchAsync();
                var time = sw.ElapsedMilliseconds;
                _output.WriteLine($"Search[{i}]: {time} ms, ({result.Items.Count}/{benchmark.GetItemCount()})");
            }
        }
    }


    public class SearchBenchmark
    {
        private static readonly string _folderRoot = @"F:\";
        private static readonly string _keyword = "test";
        private FileSearchEngine _engine;

        public class TestSearchContext : ISearchContext
        {
#pragma warning disable CS0067
            public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

            public bool AllowFolder { get; set; } = true;
        }


        public SearchBenchmark()
        {
            _engine = CreateSearchEngine(_folderRoot);
        }

        public int GetItemCount()
        {
            return _engine.Tree.CollectFileContents().Count();
        }

        public async Task<SearchResult<FileContent>> SearchAsync()
        {
            return await _engine.SearchAsync(_keyword, CancellationToken.None);
        }

        private FileSearchEngine CreateSearchEngine(params string[] paths)
        {
            var engine = new FileSearchEngine(new TestSearchContext());
            //SearchEngine.Logger.SetLevel(SourceLevels.All);
            engine.AddSearchAreas(paths.Select(e => new FileArea(e, true)).ToArray());
            //engine.CommandEngineLogger.SetLevel(SourceLevels.All);
            return engine;
        }
    }

}
