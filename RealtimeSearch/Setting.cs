using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

//using YamlDotNet;
using System.Collections.ObjectModel;
using System.Xml;
using System.ComponentModel;

namespace RealtimeSearch
{
    //[Serializable]
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
        private bool _IsMonitorClipboard;
        public bool IsMonitorClipboard
        {
            get { return _IsMonitorClipboard; }
            set { _IsMonitorClipboard = value; OnPropertyChanged(); }
        }
        #endregion


        #region Property: IsTopmost
        [DataMember]
        private bool _IsTopmost;
        public bool IsTopmost
        {
            get { return _IsTopmost; }
            set { _IsTopmost = value; OnPropertyChanged(); }
        }
        #endregion

        [DataMember]
        public WINDOWPLACEMENT? WindowPlacement { set; get; }

        //[YamlDotNet.Serialization.YamlIgnore]
        //public string Path { set; get; }


        //----------------------------------------------------------------------------
        private void Constructor()
        {
            SearchPaths = new ObservableCollection<string>();
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


        //
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

    }

}
