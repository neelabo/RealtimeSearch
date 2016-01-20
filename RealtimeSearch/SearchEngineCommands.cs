// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealtimeSearch
{
	// command base
    public abstract class SearchEngineCommand
    {
        public SearchEngine SearchEngine { get; set; }
        public int SerialNumber { get; set; }
        public bool IsCancel { get; set; }
        public abstract void Exec();
    }


	// index request
    public class IndexCommand : SearchEngineCommand
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


	// re-index request
    public class ReIndexCommand : SearchEngineCommand
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


	// add path requestr
    public class AddIndexCommand : SearchEngineCommand
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


	// remove path request
    public class RemoveIndexCommand : SearchEngineCommand
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


	// search request
    public class SearchCommand : SearchEngineCommand
    {
        public string Keyword { get; set; }
        public bool IsSearchFolder { get; set; }
        public override void Exec()
        {
            SearchEngine.CommandSearch(Keyword, IsSearchFolder);
        }
        public override string ToString()
        {
            return $"Search.{SerialNumber}:{Keyword}";
        }
    }
}
