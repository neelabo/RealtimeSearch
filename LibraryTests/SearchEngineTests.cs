using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeeLaboratory.IO.Search;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeeLaboratory.IO.Search.Tests
{
    [TestClass()]
    public class SearchEngineTests
    {
        private TestContext _testContext;
        public TestContext TestContext
        {
            get => _testContext;
            set => _testContext = value;
        }


        private static string _folderRoot = @"TestFolders";
        private static string _folderSub1 = @"TestFolders\SubFolder1";
        private static string _folderSub2 = @"TestFolders\SubFolder2";



        [TestMethod()]
        public async Task SearchEngineTest()
        {
            // 初期化
            var engine = new SearchEngine();
            engine.Start();
            //engine.Progress = new SearchEngineProgress(); // 状態通知用。 IProgress

            // 検索パス設定
            string[] areas =  //new string[]
            {
                _folderSub1,
                _folderRoot,
            };
            engine.SetSearchAreas(areas);
            //engine.SearchAreas = areas;


            // 検索１：通常検索
            SearchOption option = new SearchOption() { IsPerfect = false };

            SearchResult result = await engine.SearchAsync("File1", option);

            // 結果表示
            foreach (var item in result.Items)
            {
                _testContext.WriteLine($"{item.Name}");
            }


#if false
            // 検索2：イベントによるコールバック
            engine.Searched += (s, e) => Debug.WriteLine("Searched!");
            engine.SearchAsync("keyword", option); // なげっぱ

            // 検索3：タスクによる
            Task task = engine.SearchAsync("keyword", option);
            task.ContinueWith<SearchResult>(() => are); かな？

            // フォルダ状態変更等による結果変更の監視
            var watcher = new SearchResultWatcher(engine, result);
            watcher.Start();
            // watcher.Stop();
#endif
        }


        private static string _fileAppend1 = @"TestFolders\SubFolder1\append1.txt";
        private static string _fileAppend2 = @"TestFolders\SubFolder1\append2.bin";


        public SearchEngine CreateTestEnvironment()
        {
            // 不要ファイル削除
            if (File.Exists(_fileAppend1)) File.Delete(_fileAppend1);

            // エンジン初期化
            var engine = new SearchEngine();
            engine.SearchAreas.Add(_folderRoot);
            engine.SearchAreas.Add(_folderSub1);
            engine.SearchAreas.Add(_folderSub2);
            engine.Start();

            return engine;
        }


        [TestMethod()]
        public async Task SearchEngineAreaTest()
        {
            var engine = new SearchEngine();
            engine.Start();

            // パスの追加
            engine.SearchAreas.Add(_folderRoot);

            // 全てのコマンドが全て処理されるまで待機
            await engine.WaitAsync();

            // 反映された情報の確認
            int nodeCount = engine.NodeCount;
            _testContext.WriteLine($"NodeCount: {nodeCount}");
            Debug.Assert(nodeCount > 0);

            engine.Stop();
        }


        [TestMethod()]
        public async Task SearchEngineSearchTest()
        {
            var engine = CreateTestEnvironment();

            var result = await engine.SearchAsync("file", new SearchOption());
            Debug.Assert(result.Items.Count == 9);
        }


        [TestMethod()]
        public async Task SearchEngineWatchResultTest()
        {
            var engine = CreateTestEnvironment();

            var result = await engine.SearchAsync(".txt", new SearchOption());
            var resultCount = result.Items.Count;

            var watcher = new SearchResultWatcher(engine, result);
            watcher.Start();

            // ファイル追加 ...
            using (FileStream stream = File.Create(_fileAppend1)) { }
            using (FileStream stream = File.Create(_fileAppend2)) { }
            await Task.Delay(100);
            Debug.Assert(result.Items.Count == resultCount + 1);

            // 名前変更
            var fileAppend2Ex = Path.ChangeExtension(_fileAppend2, ".txt");
            File.Move(_fileAppend2, fileAppend2Ex);
            await Task.Delay(100);
            Debug.Assert(result.Items.Count == resultCount + 2);

            // 内容変更
            using (FileStream stream = File.Open(fileAppend2Ex, FileMode.Append))
            {
                stream.WriteByte(0x00);
            }
            await Task.Delay(100);
            var item = result.Items.First(e => e.Path == fileAppend2Ex);
            Debug.Assert(item.FileInfo.Size == 1);

            // ファイル削除...
            File.Delete(_fileAppend1);
            File.Delete(fileAppend2Ex);
            await Task.Delay(100);
            await engine.WaitAsync();

            // 戻ったカウント確認
            Debug.Assert(result.Items.Count == resultCount);

            watcher.Stop();
        }


#if false
        [TestMethod()]
        public void SearchEngineTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void StartTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void IndexRequestTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ReIndexRequestTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void AddIndexRequestTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void RemoveIndexRequestTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void RenameIndexRequestTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void RefleshIndexRequestTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void SearchRequestTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void CommandIndexTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void CommandReIndexTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void CommandAddIndexTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void CommandRemoveIndexTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void CommandRenameIndexTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void CommandRefleshIndexTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void CommandSearchTest()
        {
            Assert.Fail();
        }
#endif
    }

}