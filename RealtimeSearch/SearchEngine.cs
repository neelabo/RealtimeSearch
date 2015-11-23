using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
            return $"{SerialNumber} - Index";
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
            return $"{SerialNumber} - ReIndex";
        }
    }

    public class AddIndexCommand : MyCommand
    {
        public string Root { get; set; }
        public List<string> Paths { get; set; } = new List<string>();
        public override void Exec()
        {
            SearchEngine.CommandAddIndex(Root, Paths);
        }
        public override string ToString()
        {
            return $"{SerialNumber} - AdddIndex Count={Paths.Count}";
        }
    }

    public class RemoveIndexCommand : MyCommand
    {
        public string Root { get; set; }
        public string Path { get; set; }
        public override void Exec()
        {
            SearchEngine.CommandRemoveIndex(Root, Path); 
        }
        public override string ToString()
        {
            return $"{SerialNumber} - RemoveIndex {Path}";
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
        Search, // 処理中です。
        SearchResult,
        SearchResultEmpty, // "条件に一致する項目はありません。";
    }


    public class SearchEngine : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion

        //public SearchEngineState State

        #region Property: State
        private SearchEngineState _State;
        public SearchEngineState State
        {
            get { return _State; }
            set { /*Debug.WriteLine($"State = {value}");*/  _State = value; OnPropertyChanged(); OnPropertyChanged("StateMessage"); }
        }
        #endregion

        public string StateMessage
        {
            get
            {
                if (_State == SearchEngineState.Search)
                    return "処理中です...";
                else if (_State == SearchEngineState.SearchResultEmpty)
                    return "条件に一致する項目はありません。";
                else
                    return null;
            }
        }


        #region Property: Command
        private MyCommand _Command;
        public MyCommand Command
        {
            get { return _Command; }
            set { _Command = value; OnPropertyChanged(); }
        }
        #endregion


        #region Property: CommandCount
        private int _CommandCount;
        public int CommandCount
        {
            get { return _CommandCount; }
            set { _CommandCount = value; OnPropertyChanged(); }
        }
        #endregion



        public string CurrentKeyword { get; private set; }


        private SemaphoreSlim CountSemaphore;

        private Object thisLock = new Object();

        private List<MyCommand> CommandList;

        private int SerialNumber;

        private Task WorkerTask;


        public Index Index;


        //
        //public event EventHandler ResultChanged;
        public event EventHandler<int> ResultChanged;


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


        private void AddCommand(MyCommand command)
        {
            command.SearchEngine = this;
            command.SerialNumber = SerialNumber++;
            CommandList.Add(command);
            CommandCount = CommandList.Count;

            CountSemaphore.Release();
        }


        // インデックス化リクエスト
        public void IndexRequest(string[] paths)
        {
            lock (thisLock)
            {
                CommandList.ForEach(cmd => { if (cmd is IndexCommand || cmd is ReIndexCommand) cmd.IsCancel = true; });

                AddCommand(new IndexCommand() { Paths = paths });
            }
        }


        // 再インデックス化リクエスト
        public void ReIndexRequest()
        {
            lock (thisLock)
            {
                if (CommandList.Any(cmd => cmd is IndexCommand || cmd is ReIndexCommand)) return;

                AddCommand(new ReIndexCommand());
            }
        }


        public void AddIndexRequest(string root, string path)
        {
            lock (thisLock)
            {
                if (CommandList.Any(cmd => cmd is IndexCommand || cmd is ReIndexCommand)) return;

                // コマンドをまとめる
                AddIndexCommand command = (CommandList.Count >= 1) ? CommandList.LastOrDefault(c => c is AddIndexCommand && ((AddIndexCommand)c).Root == root) as AddIndexCommand : null;
                if (command != null && command.Root == root)
                {
                    command.Paths.Add(path);
                }
                else
                {
                    command = new AddIndexCommand() { Root = root };
                    command.Paths.Add(path);

                    AddCommand(command);
                }
            }
        }

        public void RemoveIndexRequest(string root, string path)
        {
            lock (thisLock)
            {
                if (CommandList.Any(cmd => cmd is IndexCommand || cmd is ReIndexCommand)) return;

                AddCommand(new RemoveIndexCommand() { Root = root, Path = path });
            }
        }




        // 検索リクエスト
        public void SearchRequest(string keyword)
        {
            keyword = keyword ?? "";
            keyword = keyword.Trim();

            lock (thisLock)
            {
                //Debug.WriteLine($"Request: {keyword} ...");

                CommandList.ForEach(cmd => { if (cmd is SearchCommand) cmd.IsCancel = true; });
                //CommandList.RemoveAll(cmd => cmd is SearchCommand); // 消すのはダメ

                // 他のコマンドが存在する場合のみ表示更新
                if (Command != null || CommandList.Count > 1)
                {
                    ResultChanged?.Invoke(this, 0);
                    State = string.IsNullOrEmpty(keyword) ? SearchEngineState.None : SearchEngineState.Search;
                }

                //CommandList.Add(new SearchCommand() { SearchEngine = this, SerialNumber = this.SerialNumber++, Keyword = keyword });
                //CountSemaphore.Release();
                AddCommand(new SearchCommand() { Keyword = keyword });

                //Debug.WriteLine($"Request: {keyword} Done.");
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

                    CommandCount = CommandList.Count;
                }

                //CommandMessage = Command.ToString(); // ##

                // 処理
                if (Command.IsCancel) continue;
#if DEBUG
                await Task.Delay(100);
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

                //CommandMessage = null; // ##
            }
        }

        public void CommandIndex(string[] paths)
        {
            //State = SearchEngineState.Index;
            Index.Collect(paths);
            //State = SearchEngineState.None;
        }

        public void CommandReIndex()
        {
            //State = SearchEngineState.Index;
            Index.Collect();
            //State = SearchEngineState.None;
        }

        public void CommandAddIndex(string root, List<string> paths)
        {
            //Debug.WriteLine($"Index Add: {path}");
            Index.AddPath(root, paths);
        }

        public void CommandRemoveIndex(string root, string path)
        {
            //Debug.WriteLine($"Index Remove: {path}");
            Index.RemovePath(root, path);
        }


        public void CommandSearch(string keyword)
        {
            //Debug.WriteLine($"Search: {keyword} ...");

            //State = SearchEngineState.Search;
            Index.Check(keyword);
            //State = SearchEngineState.None;

            lock(thisLock)
            {
                if (CommandList.Any(n => n is SearchCommand))
                {
                    State = SearchEngineState.Search;
                }
                else if (Index.Matches.Count <= 0)
                {
                    State = string.IsNullOrEmpty(keyword) ? SearchEngineState.None : SearchEngineState.SearchResultEmpty;
                    CurrentKeyword = keyword;
                    ResultChanged?.Invoke(this, Index.Matches.Count);
                }
                else
                {
                    State = SearchEngineState.SearchResult;
                    CurrentKeyword = keyword;
                    ResultChanged?.Invoke(this, Index.Matches.Count);
                }
            }

            //Debug.WriteLine($"Search: {keyword} Done.");
        }
    }
}
