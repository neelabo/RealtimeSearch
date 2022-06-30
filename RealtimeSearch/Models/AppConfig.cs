using NeeLaboratory.IO.Search;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    [DataContract]
    public class ListViewColumnMemento
    {
        [DataMember]
        public string Header { get; set; } = "";
        [DataMember]
        public double Width { get; set; }
    }



    public class AppConfig : BindableBase
    {

        private bool _isMonitorClipboard;
        private bool _isTopmost;
        private bool _isDetailVisibled;
        private NeeLaboratory.IO.Search.SearchOption _searchOption;


        public AppConfig()
        {
            SearchAreas = new ObservableCollection<SearchArea>();
            IsMonitorClipboard = true;
            _searchOption = new NeeLaboratory.IO.Search.SearchOption();
            ExternalPrograms = new List<ExternalProgram>();
            ExternalPrograms.Add(new ExternalProgram());
            ExternalPrograms.Add(new ExternalProgram());
            ExternalPrograms.Add(new ExternalProgram());
            WebSearchFormat = "https://www.google.co.jp/search?q=$(query)";
        }


        public ObservableCollection<SearchArea> SearchAreas { get; set; }

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


        public NeeLaboratory.IO.Search.SearchOption SearchOption
        {
            get { return _searchOption; }
            set { if (_searchOption != value) { _searchOption = value; RaisePropertyChanged(); } }
        }

        public bool IsDetailVisibled
        {
            get { return _isDetailVisibled; }
            set { if (_isDetailVisibled != value) { _isDetailVisibled = value; RaisePropertyChanged(); } }
        }

        public string WebSearchFormat { set; get; }

        public List<ExternalProgram> ExternalPrograms { set; get; }

        public List<ListViewColumnMemento> ListViewColumnMemento { get; set; } = new List<ListViewColumnMemento>();

        public WindowPlacement.WINDOWPLACEMENT WindowPlacement { get; set; }



        public void ToggleAllowFolder()
        {
            SearchOption.AllowFolder = !SearchOption.AllowFolder;
            RaisePropertyChanged("SearchOption.AllowFolder");
        }
    }
}
