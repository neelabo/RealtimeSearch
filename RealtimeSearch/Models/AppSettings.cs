using CommunityToolkit.Mvvm.ComponentModel;
using NeeLaboratory.IO.Search.Files;
using NeeLaboratory.RealtimeSearch.Services;
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


    public class AppSettings : ObservableObject, ISearchContext
    {
        private string _language = CultureInfo.CurrentCulture.Name;
        private bool _monitorClipboard = true;
        private bool _topmost;
        private bool _includeFolders = true;
        private string _webSearchFormat = "https://www.google.com/search?q=$(query)";
        private bool _showDetail;
        private bool _useCache = true;
        private ObservableCollection<ExternalProgram> _externalPrograms;


        public AppSettings()
        {
            Format = new FormatVersion(ApplicationInfo.Current.CreateFormatName("Settings"), 4, 0, 0);
            _externalPrograms = new();
            AttachExternalPrograms();
        }

        public FormatVersion Format { get; set; }

        public string Language
        {
            get { return _language; }
            set { SetProperty(ref _language, value); }
        }

        public bool IncludeFolders
        {
            get { return _includeFolders; }
            set { SetProperty(ref _includeFolders, value); }
        }

        public bool MonitorClipboard
        {
            get { return _monitorClipboard; }
            set { SetProperty(ref _monitorClipboard, value); }
        }

        public bool Topmost
        {
            get { return _topmost; }
            set { SetProperty(ref _topmost, value); }
        }

        public bool UseCache
        {
            get { return _useCache; }
            set { SetProperty(ref _useCache, value); }
        }

        public string WebSearchFormat
        {
            get { return _webSearchFormat; }
            set { SetProperty(ref _webSearchFormat, value); }
        }

        public bool ShowDetail
        {
            get { return _showDetail; }
            set { SetProperty(ref _showDetail, value); }
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

        public List<ListViewColumnMemento> ListLayout { get; set; } = [];


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
