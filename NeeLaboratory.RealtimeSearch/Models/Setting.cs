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
using System.Windows;
using System.Xml;

namespace NeeLaboratory.RealtimeSearch
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
        #region INotifyPropertyChanged Support

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value;
            this.RaisePropertyChanged(propertyName);
            return true;
        }

        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void AddPropertyChanged(string propertyName, PropertyChangedEventHandler handler)
        {
            PropertyChanged += (s, e) => { if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == propertyName) handler?.Invoke(s, e); };
        }

        #endregion


        [DataMember]
        public ObservableCollection<string> SearchPaths { set; get; }

        #region Property: IsMonitorClipboard
        [DataMember(Name = nameof(IsMonitorClipboard))]
        private bool _isMonitorClipboard;
        public bool IsMonitorClipboard
        {
            get { return _isMonitorClipboard; }
            set { _isMonitorClipboard = value; RaisePropertyChanged(); }
        }
        #endregion


        #region Property: IsTopmost
        [DataMember(Name = nameof(IsTopmost))]
        private bool _isTopmost;
        public bool IsTopmost
        {
            get { return _isTopmost; }
            set { _isTopmost = value; RaisePropertyChanged(); }
        }
        #endregion


        /// <summary>
        /// SearchOption property. (Legacy)
        /// </summary>
        [Obsolete, DataMember(Name = "SearchOption", EmitDefaultValue = false)]
        private IO.Search.SearchOptionLegacyV1 _searchOptionLegacyV1;

        /// <summary>
        /// SearchOption property.
        /// </summary>
        private NeeLaboratory.IO.Search.SearchOption _searchOption;
        [DataMember(Name = "SearchOptionV2")]
        public NeeLaboratory.IO.Search.SearchOption SearchOption
        {
            get { return _searchOption; }
            set { if (_searchOption != value) { _searchOption = value; RaisePropertyChanged(); } }
        }

        /// <summary>
        /// IsDetailVisibled property.
        /// </summary>
        [DataMember(Name = nameof(IsDetailVisibled))]
        private bool _isDetailVisibled;
        public bool IsDetailVisibled
        {
            get { return _isDetailVisibled; }
            set { if (_isDetailVisibled != value) { _isDetailVisibled = value; RaisePropertyChanged(); } }
        }

        [DataMember]
        public Rect WindowRect { get; set; }

        [DataMember]
        public string WebSearchFormat { set; get; }

        [DataMember]
        public List<ExternalProgram> ExternalPrograms { set; get; }

        [DataMember]
        public List<ListViewColumnMemento> ListViewColumnMemento { get; set; }


        //----------------------------------------------------------------------------
        private void Constructor()
        {
            SearchPaths = new ObservableCollection<string>();
            IsMonitorClipboard = true;
            SearchOption = new NeeLaboratory.IO.Search.SearchOption();
            ExternalPrograms = new List<ExternalProgram>();
            ExternalPrograms.Add(new ExternalProgram());
            ExternalPrograms.Add(new ExternalProgram());
            ExternalPrograms.Add(new ExternalProgram());
            WebSearchFormat = "https://www.google.co.jp/search?q=$(query)";
            WindowRect = Rect.Empty;
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

        [OnDeserialized]
        private void Deserialized(StreamingContext c)
        {
#pragma warning disable CS0612
            if (_searchOptionLegacyV1 != null)
            {
                _searchOption.SearchMode = _searchOptionLegacyV1.IsOptionEnabled ? IO.Search.SearchMode.Advanced : IO.Search.SearchMode.Simple;
                _searchOption.AllowFolder = _searchOptionLegacyV1.AllowFolder;
                _searchOptionLegacyV1 = null;
            }
#pragma warning restore CS0612
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


        //
        public static Setting Load(string path)
        {
            using (XmlReader xr = XmlReader.Create(path))
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(Setting));
                Setting setting = (Setting)serializer.ReadObject(xr);
                return setting;
            }
        }

        /// <summary>
        /// 1.5互換
        /// namespace変更に伴う読み込みエラー修正
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Setting LoadEx(string path)
        {
            // 全部読み込み
            string text;
            using (StreamReader sr = new StreamReader(path))
            {
                text = sr.ReadToEnd();
            }

            // namespace置換
            text = text.Replace("http://schemas.datacontract.org/2004/07/RealtimeSearch.Search", "http://schemas.datacontract.org/2004/07/NeeLaboratory.IO.Search");
            text = text.Replace("http://schemas.datacontract.org/2004/07/RealtimeSearch", "http://schemas.datacontract.org/2004/07/NeeLaboratory.RealtimeSearch");

            using (var reader = new StringReader(text))
            using (XmlReader xr = XmlReader.Create(reader))
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
                try
                {
                    try
                    {
                        return Setting.Load(path);
                    }
                    catch (SerializationException)
                    {
                        return Setting.LoadEx(path);
                    }
                }
                catch (Exception)
                {
                    System.Windows.MessageBox.Show("設定が読み込めませんでした。初期設定で起動します。", "読み込み失敗", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return new Setting();
                }
            }
            else
            {
                return new Setting();
            }
        }


        public void SetSearchMode(IO.Search.SearchMode mode)
        {
            SearchOption.SearchMode = mode;
            RaisePropertyChanged("SearchOption.SearchMode");
        }

        public void ToggleAllowFolder()
        {
            SearchOption.AllowFolder = !SearchOption.AllowFolder;
            RaisePropertyChanged("SearchOption.AllowFolder");
        }
    }
}
