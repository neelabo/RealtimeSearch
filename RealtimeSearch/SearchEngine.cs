using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealtimeSearch
{
    public abstract class MyCommand
    {
        public SearchEngine SearchEngine { get; set; }
        public bool IsCancel { get; set; }
        public abstract void Exec();
    }


    public class IndexCommand : MyCommand
    {
        public string[] Paths { get; set; }
        public override void Exec()
        {
            SearchEngine.CommandIndex(Paths);
        }
    }


    public class SearchCommand : MyCommand
    {
        public string Keyword { get; set; }
        public override void Exec()
        {
            SearchEngine.CommandSearch(Keyword);
        }
    }



    public class SearchEngine
    {


        private SemaphoreSlim CountSemaphore;

        private Object thisLock = new Object();

        private List<MyCommand> CommandList;

        private Task WorkerTask;


        public Index Index;


        //
        public event EventHandler ResultChanged;


        //
        public SearchEngine()
        {
            Index = new Index();

            CountSemaphore = new SemaphoreSlim(0);

            CommandList = new List<MyCommand>();

            //WorkerTask = WorkAsync();
        }

        public void Start()
        {
            //WorkerTask.Start();
            WorkerTask = Task.Run(() => WorkAsync());
        }

        // インデックス化リクエスト
        public void IndexRequest(string[] paths)
        {
            lock (thisLock)
            {
                CommandList.ForEach(n => { if (n is IndexCommand) n.IsCancel = true; });

                CommandList.Add(new IndexCommand() { SearchEngine = this, Paths = paths });
                CountSemaphore.Release();
            }
        }


        // 検索リクエスト
        public void SearchRequest(string keyword)
        {
            lock (thisLock)
            {
                CommandList.ForEach(n => { if (n is SearchCommand) n.IsCancel = true; });

                CommandList.Add(new SearchCommand() { SearchEngine = this, Keyword = keyword });
                CountSemaphore.Release();
            }
        }

        // ワーカータスク
        public async Task WorkAsync()
        {
            while (true)
            {
                // コマンドがあることをSemaphoreで検知する
                await CountSemaphore.WaitAsync();

                MyCommand command;

                lock (thisLock)
                {
                    // コマンド取り出し
                    command = CommandList[0];
                    CommandList.RemoveAt(0);
                }

                // 処理
                if (command.IsCancel) continue;
                command.Exec();
            }
        }

        public void CommandIndex(string[] paths)
        {
            Index.Initialize(paths);
        }

        public void CommandSearch(string keyword)
        {
            Index.Check(keyword);
            ResultChanged?.Invoke(this, null);
        }
    }
}
