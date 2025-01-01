using NeeLaboratory.ComponentModel;
using NeeLaboratory.IO.Search.Files;
using NeeLaboratory.RealtimeSearch.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;


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
        private bool _isMonitorClipboard = true;
        private bool _isTopmost;
        private bool _isDetailVisible;
        private bool _allowFolder;
        private string _webSearchFormat = "https://www.google.com/search?q=$(query)";
        private bool _useCache = true;
        private ObservableCollection<ExternalProgram> _externalPrograms;

        public AppSettings()
        {
            _externalPrograms = new();
            AttachExternalPrograms();
        }


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

        public string WebSearchFormat
        {
            get { return _webSearchFormat; }
            set { SetProperty(ref _webSearchFormat, value); }
        }

        public bool UseCache
        {
            get { return _useCache; }
            set { SetProperty(ref _useCache, value); }
        }


        public WindowPlacement WindowPlacement { get; set; } = new WindowPlacement();

        public ObservableCollection<FileArea> SearchAreas { get; set; } = [];

        public ObservableCollection<ExternalProgram> ExternalPrograms
        {
            get { return _externalPrograms; }
            set
            {
                if (SetProperty(ref _externalPrograms, value))
                {
                    AttachExternalPrograms();
                }
            }
        }

        public List<ListViewColumnMemento> ListViewColumnMemento { get; set; } = [];


        public AppSettings Validate()
        {
            // TODO: 互換性処理
            return this;
        }

        private void AttachExternalPrograms()
        {
            if (_externalPrograms is null) return;

            _externalPrograms.CollectionChanged += (s, e) => ValidateExternalProgramId();
            ValidateExternalProgramId();
        }

        private void ValidateExternalProgramId()
        {
            for (int i = 0; i < ExternalPrograms.Count; i++)
            {
                ExternalPrograms[i].Id = i + 1;
            }
        }
    }
}
