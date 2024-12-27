using NeeLaboratory.ComponentModel;
using NeeLaboratory.IO.Search.Files;
using NeeLaboratory.RealtimeSearch.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace NeeLaboratory.RealtimeSearch.Models
{
    public class ListViewColumnMemento
    {
        public string Header { get; set; } = "";
        public double Width { get; set; }
    }


    public class AppSettings : BindableBase, ISearchContext
    {
        private string _language = CultureInfo.CurrentCulture.Name;
        private bool _isMonitorClipboard;
        private bool _isTopmost;
        private bool _isDetailVisible;
        private bool _allowFolder;


        public AppSettings()
        {
            SearchAreas = new ObservableCollection<FileArea>();
            IsMonitorClipboard = true;
            ExternalPrograms = new ObservableCollection<ExternalProgram>
            {
                new ExternalProgram(),
            };
            WebSearchFormat = "https://www.google.com/search?q=$(query)";

            Validate();
        }


        public ObservableCollection<FileArea> SearchAreas { get; set; }


        public string Language
        {
            get { return _language; }
            set { SetProperty(ref _language, value); }
        }

        public bool IsMonitorClipboard
        {
            get { return _isMonitorClipboard; }
            set { SetProperty(ref _isMonitorClipboard, value); }
        }

        public bool IsTopmost
        {
            get { return _isTopmost; }
            set { SetProperty(ref _isTopmost, value); }
        }

        public bool AllowFolder
        {
            get { return _allowFolder; }
            set { SetProperty(ref _allowFolder, value); }
        }

        public bool IsDetailVisible
        {
            get { return _isDetailVisible; }
            set { SetProperty(ref _isDetailVisible, value); }
        }

        public string WebSearchFormat { set; get; }

        public ObservableCollection<ExternalProgram> ExternalPrograms { set; get; }

        public List<ListViewColumnMemento> ListViewColumnMemento { get; set; } = new List<ListViewColumnMemento>();

        public WindowPlacement WindowPlacement { get; set; } = new WindowPlacement();

        public bool UseCache { get; set; } = true;


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

        public AppSettings Validate()
        {
            ValidateExternalProgramsIndex();
            return this;
        }

        public void ValidateExternalProgramsIndex()
        {
            for (int i = 0; i < ExternalPrograms.Count; i++)
            {
                ExternalPrograms[i].Id = i + 1;
            }
        }
    }
}
