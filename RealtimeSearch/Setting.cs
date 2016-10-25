﻿// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
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

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
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
        [DataMember]
        private bool _isMonitorClipboard;
        public bool IsMonitorClipboard
        {
            get { return _isMonitorClipboard; }
            set { _isMonitorClipboard = value; OnPropertyChanged(); }
        }
        #endregion


        #region Property: IsTopmost
        [DataMember]
        private bool _isTopmost;
        public bool IsTopmost
        {
            get { return _isTopmost; }
            set { _isTopmost = value; OnPropertyChanged(); }
        }
        #endregion


        #region Property: IsSearchFolder
        [DataMember]
        private bool _isSearchFolder;
        public bool IsSearchFolder
        {
            get { return _isSearchFolder; }
            set { _isSearchFolder = value; OnPropertyChanged(); }
        }
        #endregion


        [DataMember]
        public WINDOWPLACEMENT? WindowPlacement { set; get; }

        [DataMember]
        public string WebSearchFormat { set; get; }


        #region Property: ExternalApplication
        private string _externalApplication;
        [DataMember]
        public string ExternalApplication
        {
            get { return _externalApplication; }
            set { _externalApplication = value.Trim(); OnPropertyChanged(); }
        }
        #endregion


        #region Property: ExternalApplicationParam
        private string _externalApplicationParam;
        [DataMember]
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
                OnPropertyChanged();
            }
        }
        #endregion


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
    }
}
