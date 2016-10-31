// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace RealtimeSearch
{
    [DataContract]
    public class ListViewColumnMemento
    {
        [DataMember]
        public string Header { get; set; }
        [DataMember]
        public double Width { get; set; }
    }

    [DataContract]
    public class Setting : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(name));
            }
        }
        #endregion

        [DataMember]
        public ObservableCollection<string> SearchPaths { set; get; }

        #region Property: IsMonitorClipboard
        [DataMember (Name = nameof(IsMonitorClipboard))]
        private bool _isMonitorClipboard;
        public bool IsMonitorClipboard
        {
            get { return _isMonitorClipboard; }
            set { _isMonitorClipboard = value; RaisePropertyChanged(); }
        }
        #endregion


        #region Property: IsTopmost
        [DataMember (Name =nameof(IsTopmost))]
        private bool _isTopmost;
        public bool IsTopmost
        {
            get { return _isTopmost; }
            set { _isTopmost = value; RaisePropertyChanged(); }
        }
        #endregion


        #region Property: IsSearchFolder
        [DataMember (Name = nameof(IsSearchFolder))]
        private bool _isSearchFolder;
        public bool IsSearchFolder
        {
            get { return _isSearchFolder; }
            set { _isSearchFolder = value; RaisePropertyChanged(); }
        }
        #endregion

        /// <summary>
        /// IsDetailVisibled property.
        /// </summary>
        [DataMember (Name =nameof(IsDetailVisibled))]
        private bool _isDetailVisibled;
        public bool IsDetailVisibled
        {
            get { return _isDetailVisibled; }
            set { if (_isDetailVisibled != value) { _isDetailVisibled = value; RaisePropertyChanged(); } }
        }



        [DataMember]
        public WINDOWPLACEMENT? WindowPlacement { set; get; }

        [DataMember]
        public string WebSearchFormat { set; get; }


        #region Property: ExternalApplication
        [DataMember (Name = nameof(ExternalApplication))]
        private string _externalApplication;
        public string ExternalApplication
        {
            get { return _externalApplication; }
            set { _externalApplication = value.Trim(); RaisePropertyChanged(); }
        }
        #endregion


        #region Property: ExternalApplicationParam
        [DataMember(Name = nameof(ExternalApplicationParam))]
        private string _externalApplicationParam;
        public string ExternalApplicationParam
        {
            get { return _externalApplicationParam; }
            set
            {
                var s = value.Trim();
                if (!s.Contains("$(file)"))
                {
                    s = (s + " \"$(file)\"").Trim();
                }
                _externalApplicationParam = s;
                RaisePropertyChanged();
            }
        }
        #endregion


        /// <summary>
        /// ExternalApplicationFilter property.
        /// </summary>
        private string _externalApplicationFilter;
        [DataMember(Name =nameof(ExternalApplicationFilter))]
        public string ExternalApplicationFilter
        {
            get { return _externalApplicationFilter; }
            set { if (_externalApplicationFilter != value) { _externalApplicationFilter = value; RaisePropertyChanged(); SetExternalApplicationFilterExtensions(_externalApplicationFilter); } }
        }

        public List<string> _externalApplicationFilterExtensions { get; set; }

        //
        private void SetExternalApplicationFilterExtensions(string exts)
        { 
            if (exts == null) return;

            var reg = new Regex(@"^[\*\.\s]+");

            var list = new List<string>();
            foreach (var token in exts.Split(';'))
            {
                //var ext = token.Trim().TrimStart('.').ToLower();
                var ext = reg.Replace(token.Trim(), "").ToLower();
                if (!string.IsNullOrWhiteSpace(ext)) list.Add("." + ext);
            }

            _externalApplicationFilterExtensions = list;
        }

        //
        public bool CheckExternalApplicationFilter(string input)
        {
            if (input == null) return false;
            if (_externalApplicationFilterExtensions == null || _externalApplicationFilterExtensions.Count == 0) return true;

            var ext = System.IO.Path.GetExtension(input).ToLower();
            return _externalApplicationFilterExtensions.Contains(ext);
        }



        [DataMember]
        public List<ListViewColumnMemento> ListViewColumnMemento { get; set; }



        //----------------------------------------------------------------------------
        private void Constructor()
        {
            SearchPaths = new ObservableCollection<string>();
            IsMonitorClipboard = true;
            IsSearchFolder = true;
            ExternalApplication = "";
            ExternalApplicationParam = "";
            WebSearchFormat = "https://www.google.co.jp/search?q=$(query)";
        }


        public Setting()
        {
            Constructor();
        }


        [OnDeserializing]
        private void Deserializing(StreamingContext c)
        {
            Constructor();
        }


        public void Save(string path)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = new System.Text.UTF8Encoding(false);
            settings.Indent = true;
            using (XmlWriter xw = XmlWriter.Create(path, settings))
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(Setting));
                serializer.WriteObject(xw, this);
            }
        }


        public static Setting Load(string path)
        {
            using (XmlReader xr = XmlReader.Create(path))
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(Setting));
                Setting setting = (Setting)serializer.ReadObject(xr);
                return setting;
            }
        }

        public static Setting LoadOrDefault(string path)
        {

            // 設定の読み込み
            if (System.IO.File.Exists(path))
            {
                return Setting.Load(path);
                //Models.Default.ReIndex(Setting.SearchPaths.ToArray());
            }
            else
            {
                return new Setting();
            }
        }
    }
}
