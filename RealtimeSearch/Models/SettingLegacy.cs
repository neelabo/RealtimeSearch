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
using System.Xml;

namespace NeeLaboratory.RealtimeSearch
{
    /// <summary>
    /// 古い形式の設定データ
    /// </summary>
    /// <remarks>
    /// サブクラスは当面共用する。変更され問題が発生したときに対処ｐする
    /// </remarks>
    [Obsolete, DataContract(Name = "Setting")]
    public class SettingLegacy
    {
        // legacy
        [Obsolete, DataMember(EmitDefaultValue = false)]
        private ObservableCollection<string>? SearchPaths;

        [DataMember]
        public ObservableCollection<SearchArea> SearchAreas { get; private set; }

        [DataMember]
        public bool IsMonitorClipboard { get; private set; }

        [DataMember]
        public bool IsTopmost { get; private set; }

        // legacy
        [Obsolete, DataMember(Name = "SearchOption", EmitDefaultValue = false)]
        private SearchOptionLegacyV1? SearchOptionLegacyV1;

        [DataMember(Name = "SearchOptionV2")]
        public NeeLaboratory.IO.Search.SearchOption SearchOption { get; private set; }

        [DataMember]
        public bool IsDetailVisibled { get; private set; }

        [DataMember]
        public string WebSearchFormat { set; private get; }

        [DataMember]
        public List<ExternalProgram> ExternalPrograms { set; private get; }

        [DataMember]
        public List<ListViewColumnMemento> ListViewColumnMemento { get; private set; } = new List<ListViewColumnMemento>();


        [MemberNotNull(nameof(SearchAreas))]
        [MemberNotNull(nameof(SearchOption))]
        [MemberNotNull(nameof(ExternalPrograms))]
        [MemberNotNull(nameof(WebSearchFormat))]
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


        public SettingLegacy()
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
                SearchOption.AllowFolder = SearchOptionLegacyV1.AllowFolder;
                SearchOptionLegacyV1 = null;
            }
#pragma warning restore CS0612
        }

        [Obsolete]
        public void Save(string path)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = new System.Text.UTF8Encoding(false);
            settings.Indent = true;
            using (XmlWriter xw = XmlWriter.Create(path, settings))
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(SettingLegacy));
                serializer.WriteObject(xw, this);
            }
        }

        public static SettingLegacy? Load(string path)
        {
            if (!File.Exists(path)) return null;

            try
            {
                using (XmlReader xr = XmlReader.Create(path))
                {
                    DataContractSerializer serializer = new DataContractSerializer(typeof(SettingLegacy));
                    var setting = serializer.ReadObject(xr) as SettingLegacy;
                    if (setting is null) throw new FormatException();
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
                    DataContractSerializer serializer = new DataContractSerializer(typeof(SettingLegacy));
                    var setting = serializer.ReadObject(xr) as SettingLegacy;
                    return setting;
                }
            }
        }

        public AppConfig ConvertToAppConfig()
        {
            var setting = new AppConfig();

            setting.SearchAreas = this.SearchAreas;
            setting.IsMonitorClipboard = this.IsMonitorClipboard;
            setting.IsTopmost = this.IsTopmost;
            setting.SearchOption = this.SearchOption;
            setting.IsDetailVisibled = this.IsDetailVisibled;
            setting.WebSearchFormat = this.WebSearchFormat;
            setting.ExternalPrograms = this.ExternalPrograms;
            setting.ListViewColumnMemento = this.ListViewColumnMemento;

            return setting;
        }

    }
}
