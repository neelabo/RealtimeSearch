using NeeLaboratory.ComponentModel;
using NeeLaboratory.IO.Search.Files;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace NeeLaboratory.RealtimeSearch
{
    public class ListViewColumnMemento
    {
        public string Header { get; set; } = "";
        public double Width { get; set; }
    }


    public class AppConfig : BindableBase, ISearchContext
    {

        private bool _isMonitorClipboard;
        private bool _isTopmost;
        private bool _isDetailVisible;
        private bool _allowFolder;


        public AppConfig()
        {
            SearchAreas = new ObservableCollection<FileArea>();
            IsMonitorClipboard = true;
            ExternalPrograms = new List<ExternalProgram>
            {
                new ExternalProgram(),
                new ExternalProgram(),
                new ExternalProgram()
            };
            WebSearchFormat = "https://www.google.co.jp/search?q=$(query)";
        }


        public ObservableCollection<FileArea> SearchAreas { get; set; }

        public bool IsMonitorClipboard
        {
            get { return _isMonitorClipboard; }
            set { _isMonitorClipboard = value; RaisePropertyChanged(); }
        }

        public bool IsTopmost
        {
            get { return _isTopmost; }
            set { _isTopmost = value; RaisePropertyChanged(); }
        }

        public bool AllowFolder
        {
            get { return _allowFolder; }
            set { SetProperty(ref _allowFolder, value); }
        }

        public bool IsDetailVisible
        {
            get { return _isDetailVisible; }
            set { if (_isDetailVisible != value) { _isDetailVisible = value; RaisePropertyChanged(); } }
        }

        public string WebSearchFormat { set; get; }

        public List<ExternalProgram> ExternalPrograms { set; get; }

        public List<ListViewColumnMemento> ListViewColumnMemento { get; set; } = new List<ListViewColumnMemento>();

        public WindowPlacement WindowPlacement { get; set; } = new WindowPlacement();


        [Obsolete("typo")] // ver.4
        [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)] 
        public bool IsDetailVisibled
        {
            get { return default; }
            set { _isDetailVisible = value; }
        }


        public void ToggleAllowFolder()
        {
            AllowFolder = !AllowFolder;
        }
    }
}
