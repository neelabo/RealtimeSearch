using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealtimeSearch
{
    public abstract class MyCommand
    {
        public SearchEngine SearchEngine { get; set; }
        public int SerialNumber { get; set; }
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
        public override string ToString()
        {
            return $"Index.{SerialNumber}";
        }
    }

    public class ReIndexCommand : MyCommand
    {
        public override void Exec()
        {
            SearchEngine.CommandReIndex();
        }
        public override string ToString()
        {
            return $"ReIndex.{SerialNumber}";
        }
    }




    public class SearchCommand : MyCommand
    {
        public string Keyword { get; set; }
        public override void Exec()
        {
            SearchEngine.CommandSearch(Keyword);
        }
        public override string ToString()
        {
            return $"Search.{SerialNumber}:{Keyword}";
        }
    }

    public enum SearchEngineState
    {
        None,
        Index,
        Search,
    }


    public class SearchEngine : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(name));
            }
        }
        #endregion

        //public SearchEngineState State

        #region Property: State
        private SearchEngineState _State;
        public SearchEngineState State
        {
            get { return _State; }
            set { _State = value; OnPropertyChanged(); }
        }
        #endregion

        #region Property: Command
        private MyCommand _Command;
        public MyCommand Command
        {
            get { return _Command; }
            set { _Command = value; OnPropertyChanged(); }
        }
        #endregion




        private SemaphoreSlim CountSemaphore;

        private Object thisLock = new Object();

        private List<MyCommand> CommandList;

        private int SerialNumber;

        private Task WorkerTask;


        public Index Index;


        //
        public event EventHandler ResultChanged;

        //

        public static SearchEngine Current { get; private set; }

        //
        public SearchEngine()
        {
            Current = this;

            Index = new Index();

            CountSemaphore = new SemaphoreSlim(0);

            CommandList = new List<MyCommand>();
        }


        public void Start()
        {
            WorkerTask = Task.Run(() => WorkAsync());
        }


        // インデックス化リクエスト
        public void IndexRequest(string[] paths)
        {
            lock (thisLock)
            {
                CommandList.ForEach(cmd => { if (cmd is IndexCommand || cmd is ReIndexCommand) cmd.IsCancel = true; });

                CommandList.Add(new IndexCommand() { SearchEngine = this, SerialNumber = this.SerialNumber++, Paths = paths });
                CountSemaphore.Release();
            }
        }


        // 再インデックス化リクエスト
        public void ReIndexRequest()
        {
            lock (thisLock)
            {
                if (CommandList.Any(cmd => cmd is IndexCommand || cmd is ReIndexCommand)) return;

                CommandList.Add(new ReIndexCommand() { SearchEngine = this, SerialNumber = this.SerialNumber++ });
                CountSemaphore.Release();
            }
        }


        // 検索リクエスト
        public void SearchRequest(string keyword)
        {
            lock (thisLock)
            {
                CommandList.ForEach(cmd => { if (cmd is SearchCommand) cmd.IsCancel = true; });

                CommandList.Add(new SearchCommand() { SearchEngine = this, SerialNumber = this.SerialNumber++, Keyword = keyword });
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

                lock (thisLock)
                {
                    // コマンド取り出し
                    Command = CommandList[0];
                    CommandList.RemoveAt(0);
                }

                // 処理
                if (Command.IsCancel) continue;
#if DEBUG
                await Task.Delay(1000);
#endif
                try
                {
                    Command.Exec();
                }
                catch (Exception e)
                {
                    // エラーはスルー
                    Debug.WriteLine(e.Message);
                }

                Command = null;
            }
        }

        public void CommandIndex(string[] paths)
        {
            State = SearchEngineState.Index;
            Index.Collect(paths);
            State = SearchEngineState.None;
        }

        public void CommandReIndex()
        {
            State = SearchEngineState.Index;
            Index.Collect();
            State = SearchEngineState.None;
        }

        public void CommandSearch(string keyword)
        {
            State = SearchEngineState.Search;
            Index.Check(keyword);
            ResultChanged?.Invoke(this, null);
            State = SearchEngineState.None;
        }
    }
}
