using NeeLaboratory.IO.Search;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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

        private bool _isMonitorClipboard;
        private bool _isTopmost;
        private bool _isDetailVisibled;
        private NeeLaboratory.IO.Search.SearchOption _searchOption;


        // legacy
        [Obsolete, DataMember(EmitDefaultValue = false)]
        private ObservableCollection<string> SearchPaths;

        [DataMember]
        public ObservableCollection<SearchArea> SearchAreas { get; set; }

        [DataMember]
        public bool IsMonitorClipboard
        {
            get { return _isMonitorClipboard; }
            set { _isMonitorClipboard = value; RaisePropertyChanged(); }
        }

        [DataMember]
        public bool IsTopmost
        {
            get { return _isTopmost; }
            set { _isTopmost = value; RaisePropertyChanged(); }
        }

        // legacy
        [Obsolete, DataMember(Name = "SearchOption", EmitDefaultValue = false)]
        private SearchOptionLegacyV1 SearchOptionLegacyV1;

        [DataMember(Name = "SearchOptionV2")]
        public NeeLaboratory.IO.Search.SearchOption SearchOption
        {
            get { return _searchOption; }
            set { if (_searchOption != value) { _searchOption = value; RaisePropertyChanged(); } }
        }

        [DataMember]
        public bool IsDetailVisibled
        {
            get { return _isDetailVisibled; }
            set { if (_isDetailVisibled != value) { _isDetailVisibled = value; RaisePropertyChanged(); } }
        }

        [DataMember]
        public string WebSearchFormat { set; get; }

        [DataMember]
        public List<ExternalProgram> ExternalPrograms { set; get; }

        [DataMember]
        public List<ListViewColumnMemento> ListViewColumnMemento { get; set; }

        [DataMember]
        public WindowPlacement.WINDOWPLACEMENT WindowPlacement { get; set; }


        //----------------------------------------------------------------------------
        private void Constructor()
        {
            SearchAreas = new ObservableCollection<SearchArea>();
            IsMonitorClipboard = true;
            SearchOption = new NeeLaboratory.IO.Search.SearchOption();
            ExternalPrograms = new List<ExternalProgram>();
            ExternalPrograms.Add(new ExternalProgram());
            ExternalPrograms.Add(new ExternalProgram());
            ExternalPrograms.Add(new ExternalProgram());
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

        [OnDeserialized]
        private void Deserialized(StreamingContext c)
        {
#pragma warning disable CS0612
            if (SearchPaths != null)
            {
                SearchAreas = new ObservableCollection<SearchArea>(SearchPaths.Select(e => new SearchArea(e, true)));
                SearchPaths = null;
            }
            if (SearchOptionLegacyV1 != null)
            {
                _searchOption.AllowFolder = SearchOptionLegacyV1.AllowFolder;
                SearchOptionLegacyV1 = null;
            }
#pragma warning restore CS0612
        }

        private static JsonSerializerOptions CreateJsonSerializerOptions()
        {
            var options = new JsonSerializerOptions();
            options.WriteIndented = true;
            options.IgnoreReadOnlyProperties = true;
            options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        public void Save(string path)
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(this, CreateJsonSerializerOptions());
            File.WriteAllBytes(path, json);
        }

        public static Setting Load(string path)
        {
            var json = File.ReadAllBytes(path);
            var readOnlySpan = new ReadOnlySpan<byte>(json);
            var setting = JsonSerializer.Deserialize<Setting>(readOnlySpan, CreateJsonSerializerOptions());
            return setting;
        }

        [Obsolete]
        public void SaveLegacy(string path)
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

        public static Setting LoadLegacy(string path)
        {
            try
            {
                using (XmlReader xr = XmlReader.Create(path))
                {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(Setting));
                    Setting setting = (Setting)serializer.ReadObject(xr);
                    return setting;
                }
            }
            catch
            {
                // NOTE: 1.5互換：namespace変更に伴う読み込みエラー修正

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
        }


        // 設定の読み込み
        public static Setting LoadOrDefault(string path, string legacyPath)
        {
            try
            {
                if (System.IO.File.Exists(path))
                {
                    return Setting.Load(path);
                }
                // NOTE: 互換処理
                else if (System.IO.File.Exists(legacyPath))
                {
                    var setting = Setting.LoadLegacy(legacyPath);

                    setting.Save(path);
                    File.Delete(legacyPath);
                    return setting;
                }
                else
                {
                    return new Setting();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                System.Windows.MessageBox.Show("設定が読み込めませんでした。初期設定で起動します。", "読み込み失敗", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return new Setting();
            }
        }

        public void ToggleAllowFolder()
        {
            SearchOption.AllowFolder = !SearchOption.AllowFolder;
            RaisePropertyChanged("SearchOption.AllowFolder");
        }
    }
}
