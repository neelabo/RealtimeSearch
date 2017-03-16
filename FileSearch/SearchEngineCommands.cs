using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeeLaboratory.IO.Search
{
    internal class CommandArgs
    {
    }

    internal class CommandBase : Utility.CommandBase
    {
        protected SearchEngine _target;

        public CommandBase(SearchEngine target)
        {
            _target = target;
        }

        protected override Task ExecuteAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// 
    /// </summary>
    internal class ResetAreaCommandArgs : CommandArgs
    {
        public string[] Area { get; set; }
    }

    /// <summary>
    /// AddAreaCommand
    /// </summary>
    internal class ResetAreaCommand : CommandBase
    {
        ResetAreaCommandArgs _args;

        public ResetAreaCommand(SearchEngine target, ResetAreaCommandArgs args) : base(target)
        {
            _args = args;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            _target.ResetArea_Execute(_args);
            await Task.Yield();
        }
    }

    /// <summary>
    /// SearchCommand Args
    /// </summary>
    internal class SearchExCommandArgs : CommandArgs
    {
        public string Keyword { get; set; }
        public SearchOption Option { get; set; }
    }

    /// <summary>
    /// SearchCommand
    /// </summary>
    internal class SearchCommand : CommandBase
    {
        private SearchExCommandArgs _args;

        public SearchResult SearchResult { get; private set; }

        public SearchCommand(SearchEngine target, SearchExCommandArgs args) : base(target)
        {
            _args = args;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            SearchResult = _target.Search_Execute(_args);
            await Task.Yield(); // ##
        }
    }

    /// <summary>
    /// WaitCommand
    /// </summary>
    internal class WaitCommand : CommandBase
    {
        public SearchResult SearchResult { get; private set; }

        public WaitCommand(SearchEngine target, CommandArgs args) : base(target)
        {
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            await Task.Yield();
        }
    }


    //
    internal enum NodeChangeType
    {
        Add,
        Remove,
        Rename,
        Reflesh,
    }

    /// <summary>
    /// NodeIndexCommandArgs 
    /// </summary>
    internal class NodeChangeCommandArgs : CommandArgs
    {
        public NodeChangeType ChangeType { get; set; }
        public string Root { get; set; }
        public string Path { get; set; }
        public string OldPath { get; set; }
    }

    /// <summary>
    /// NodeChangeCommand
    /// </summary>
    internal class NodeChangeCommand : CommandBase
    {
        private NodeChangeCommandArgs _args;

        public NodeChangeCommand(SearchEngine target, NodeChangeCommandArgs args) : base(target)
        {
            _args = args;
        }

        // タスクである必要性がない！
        protected override async Task ExecuteAsync(CancellationToken token)
        {
            _target.NodeChange_Execute(_args);
            await Task.Yield();
        }

        //
        public override string ToString()
        {
            if (_args.ChangeType == NodeChangeType.Rename)
            {
                return $"{nameof(NodeChangeCommand)}: {_args.ChangeType}: {_args.OldPath} -> {_args.Path}";
            }
            else
            {
                return $"{nameof(NodeChangeCommand)}: {_args.ChangeType}: {_args.Path}";
            }
        }
    }


 
    /// <summary>
    /// 
    /// </summary>
    public enum SearchEngineState
    {
        Idle,
        Collect,
        Search,
        Etc,
    }


    /// <summary>
    /// 
    /// </summary>
    internal class SerarchCommandEngine : Utility.CommandEngine
    {
        public SearchEngineState State
        {
            get
            {
                var current = _command;
                if (current == null)
                    return SearchEngineState.Idle;
                else if (current is ResetAreaCommand)
                    return SearchEngineState.Collect;
                else if (current is SearchCommand)
                    return SearchEngineState.Search;
                else
                    return SearchEngineState.Etc;
            }
        }
    }
}
