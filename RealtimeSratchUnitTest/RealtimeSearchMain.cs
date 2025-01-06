#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Xunit.Sdk;
using NeeLaboratory.RealtimeSearch;
using System.Diagnostics;
using System.IO;
using Xunit.Abstractions;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using NeeLaboratory.IO.Search.Diagnostics;
using System.Collections.ObjectModel;
using NeeLaboratory.IO.Search.Files;

namespace RealtimeSearchUnitTest
{
    public class RealtimeSearchMain
    {
        private static readonly string _folderRoot = @"TestFolders";
        private static readonly string _folderSub1 = @"TestFolders\SubFolder1";
        private static readonly string _folderSub2 = @"TestFolders\SubFolder2";

        private static readonly string _fileAppend1 = @"TestFolders\SubFolder1\append1.txt";
        private static readonly string _fileAppend2 = @"TestFolders\SubFolder1\append2.bin";
        private static readonly string _fileAppend1Ex = @"TestFolders\SubFolder1\append1.tmp";
        private static readonly string _fileAppend2Ex = @"TestFolders\SubFolder1\append2.txt";

        private readonly ITestOutputHelper _output;


        public RealtimeSearchMain(ITestOutputHelper output)
        {
            _output = output;
        }

        public class TestSearchContext : ISearchContext
        {
#pragma warning disable CS0067
            public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

            public bool IncludeFolders { get; set; } = true;
        }

        /// <summary>
        /// テスト環境初期化
        /// </summary>
        public static FileSearchEngine CreateTestEnvironment()
        {
            // 不要ファイル削除
            if (File.Exists(_fileAppend1)) File.Delete(_fileAppend1);
            if (File.Exists(_fileAppend2)) File.Delete(_fileAppend2);
            if (File.Exists(_fileAppend1Ex)) File.Delete(_fileAppend1Ex);
            if (File.Exists(_fileAppend2Ex)) File.Delete(_fileAppend2Ex);

            // エンジン初期化
            var engine = new FileSearchEngine(new TestSearchContext());
            //SearchEngine.Logger.SetLevel(SourceLevels.All);
            engine.AddSearchAreas(new FileArea(_folderRoot, true), new FileArea(_folderSub1, true), new FileArea(_folderSub2, true));
            //engine.CommandEngineLogger.SetLevel(SourceLevels.All);

            return engine;
        }

        [Fact]
        public void SimpleTest()
        {
            var engine = CreateTestEnvironment();
            Assert.NotNull(engine);
        }


        /// <summary>
        /// 非同期標準テスト
        /// </summary>
        [Fact]
        public async Task SearchEngineTestAsync()
        {
            // 初期化
            var engine = new FileSearchEngine(new TestSearchContext());

            // 検索パス設定
            engine.AddSearchAreas(new FileArea(_folderSub1, true));
            engine.AddSearchAreas(new FileArea(_folderRoot, true));


            // 検索１：通常検索
            var result = await engine.SearchAsync("File1", CancellationToken.None);

            // 結果表示
            foreach (var item in result.Items)
            {
                _output.WriteLine($"{item.Name}");
            }
        }


        /// <summary>
        /// 検索範囲テスト
        /// </summary>
        [Fact]
        public async Task SearchEngineAreaTest()
        {
            Development.Logger.SetLevel(SourceLevels.Verbose);

            var context = new TestSearchContext();
            var engine = new FileSearchEngine(context);

            // パスの追加
            engine.AddSearchAreas(new FileArea(_folderRoot, false));
            await engine.Tree.InitializeAsync(CancellationToken.None);
            //engine.DumpTree(true);
            Assert.Equal(6, engine.Tree.CollectFileContents().Count());

            engine.SetSearchAreas(new ObservableCollection<FileArea>() { new FileArea(_folderRoot, true) });
            await engine.Tree.InitializeAsync(CancellationToken.None);
            //engine.DumpTree(true);
            Assert.Equal(12, engine.Tree.CollectFileContents().Count());

            engine.SetSearchAreas(new ObservableCollection<FileArea>() { new FileArea(_folderRoot, true), new FileArea(_folderSub1, true) });
            await engine.Tree.InitializeAsync(CancellationToken.None);
            Assert.Equal(12, engine.Tree.CollectFileContents().Count());

            // 変則エリア。NodeTreeの結合が発生
            engine.SetSearchAreas(new ObservableCollection<FileArea>() { new FileArea(_folderRoot, false), new FileArea(_folderSub1, true) });
            await engine.Tree.InitializeAsync(CancellationToken.None);
            DumpTree(engine);
            Assert.Equal(9, engine.Tree.CollectFileContents().Count());

            context.IncludeFolders = true;
            var result = await engine.SearchAsync("SubFolder1", CancellationToken.None);
            Assert.Single(result.Items);
        }

        private void DumpTree(FileSearchEngine engine)
        {
            foreach (var item in engine.Tree.CollectFileContents())
            {
                _output.WriteLine($"{item.Path}");
            }
        }

        /// <summary>
        /// 基本検索テスト
        /// </summary>
        [Theory]
        [InlineData(9, "file")]
        [InlineData(0, "/word あいう")]
        [InlineData(1, "/word あいうえお")]
        [InlineData(0, "/word ウエオ")]
        [InlineData(1, "/word アイウエオ")]
        [InlineData(0, "/re file")]
        [InlineData(9, "/ire file")]
        [InlineData(3, "File3")]
        [InlineData(6, "File3 /or File2")]
        [InlineData(3, "file3")]
        [InlineData(1, "file3 /not sub")]
        [InlineData(12, "/since 2018-01-01")]
        [InlineData(0, "/until 2018-01-01")]
        public async Task SearchEngineSearchTest(int expected, string keyword)
        {
            var engine = CreateTestEnvironment();

            var result = await engine.SearchAsync(keyword, CancellationToken.None);

            foreach (var item in result.Items)
            {
                _output.WriteLine($"{(item.IsDirectory ? 'D' : 'F')}: {item.Path}");
            }

            Assert.Equal(expected, result.Items.Count);
        }


        /// <summary>
        /// マルチ検索テスト
        /// </summary>
        [Fact]
        public async Task SearchEngineMultiSearchTest()
        {
            var engine = CreateTestEnvironment();

            List<FileSearchResultWatcher> result;

            var keywords = new string[] { "file", "/word あいう", "/word あいうえお", "/word ウエオ" };
            var answers = new int[] { 9, 0, 1, 0 };

            result = await engine.MultiSearchAsync(keywords, CancellationToken.None);
            Assert.Equal(answers[0], result[0].Items.Count);
            Assert.Equal(answers[1], result[1].Items.Count);
            Assert.Equal(answers[2], result[2].Items.Count);
            Assert.Equal(answers[3], result[3].Items.Count);
        }


        /// <summary>
        /// ファイルシステム監視テスト
        /// </summary>
        [Fact]
        public async Task SearchEngineWatchResultTest()
        {
            var engine = CreateTestEnvironment();

            var result = await engine.SearchAsync(".txt", CancellationToken.None);
            var resultCount = result.Items.Count;

            using var watcher = new FileSearchResultWatcher(engine, result);

            // ファイル追加 ...
            using (FileStream stream = File.Create(_fileAppend1)) { }
            using (FileStream stream = File.Create(_fileAppend2)) { }

            await Task.Delay(100);
            Assert.Equal(resultCount + 1, result.Items.Count);


            // 名前変更、検索結果を変更
            var fileAppend1Ex = Path.ChangeExtension(_fileAppend1, ".tmp");
            File.Move(_fileAppend1, fileAppend1Ex);
            await Task.Delay(100);
            Assert.True(result.Items.Count == resultCount + 1); // 結果から削除しない

            // 名前変更、検索結果に追加
            var fileAppend2Ex = Path.ChangeExtension(_fileAppend2, ".txt");
            File.Move(_fileAppend2, fileAppend2Ex);
            await Task.Delay(100);
            Assert.True(result.Items.Count == resultCount + 2);

            // 内容変更
            var oldItem = result.Items.FirstOrDefault(e => e.Path == Path.GetFullPath(fileAppend2Ex));
            using (FileStream stream = File.Open(fileAppend2Ex, FileMode.Append))
            {
                stream.WriteByte(0x00);
            }
            await Task.Delay(100);
            var item = result.Items.FirstOrDefault(e => e.Path == Path.GetFullPath(fileAppend2Ex));
            Assert.NotNull(item);
            Assert.Equal(oldItem, item);
            Assert.Equal(1, item?.Size);

            // ファイル削除...
            File.Delete(fileAppend1Ex);
            File.Delete(fileAppend2Ex);
            await Task.Delay(200); // 削除反映は 100ms ほど遅延されるので
            await engine.Tree.WaitAsync(CancellationToken.None);
            await Task.Delay(100);

            // 戻ったカウント確認
            Assert.True(result.Items.Count == resultCount);
        }

    }
}
