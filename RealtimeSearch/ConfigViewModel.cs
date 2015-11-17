using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Collections.ObjectModel;

namespace RealtimeSearch
{
    public class ConfigViewModel : INotifyPropertyChanged
    {
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        #endregion

        private Config config;
        public Config Config
        {
            get { return config; }
            set
            {
                config = value;
                Initialize();
            }
        }

        public IndexPaths IndexPaths { set; get; }

        public bool IsMonitorClipboard
        {
            get { return Config.IsMonitorClipboard; }

            set
            {
                Config.IsMonitorClipboard = value;
                NotifyPropertyChanged("IsMonitorClipboard");
                IsDarty = true;
            }
        }            



        public int IndexSelect { set; get; }

        public bool IsDarty { set; get; }

        //----------------------------------------------------------------------------
        public string windowTitle;
        public string WindowTitle
        {
            get { return windowTitle; }
            set { windowTitle = value; NotifyPropertyChanged("WindowTitle"); }
        }

        //----------------------------------------------------------------------------
        public ConfigViewModel()
        {
            var s = new Config();
            Config = s;
        }

        //----------------------------------------------------------------------------
        public ConfigViewModel(Config s)
        {
            Config = s;
        }

        //----------------------------------------------------------------------------
        public void Initialize()
        {
            WindowTitle = "検索設定";

            IndexPaths = new IndexPaths();
            IndexPaths.AddPaths(Config.SearchPaths);

            NotifyPropertyChanged("IndexPaths"); // 不完全...
        }


        //----------------------------------------------------------------------------
        public void AddSearchPath(string path)
        {
            IndexPaths.Add(path);

            Config.SearchPaths = IndexPaths.ToArray();

            IsDarty = true;
        }

        //----------------------------------------------------------------------------
        public void RemoveSearchPath(string path)
        {
            IndexPaths.Remove(path);

            Config.SearchPaths = IndexPaths.ToArray();

            IsDarty = true;
        }

        //----------------------------------------------------------------------------
        public void Save(string path)
        {
            Config.Save(path);

            IsDarty = true;
        }

        //----------------------------------------------------------------------------
        public void Load(string path)
        {
            Config = Config.Load(path);

            IsDarty = true;
        }

        //----------------------------------------------------------------------------
        public void New()
        {
            Config = new Config();

            IsDarty = true;
        }

        //----------------------------------------------------------------------------
        public string GetConfigDirectory()
        {
            return (string.IsNullOrEmpty(Config.Path))
                ? System.IO.Directory.GetCurrentDirectory()
                : System.IO.Path.GetDirectoryName(Config.Path);
        }
        
    }


    // 
    //============================================================================
    public class IndexPaths : ObservableCollection<string>
    {
        public void AddPaths(string[] paths)
        {
            if (paths == null) return;

            foreach (string path in paths)
            {
                this.Add(path);
            }
        }

        public new void Add(string path)
        {
            // 重複チェック
            if (this.Contains(path)) return;

            base.Add(path);
        }
    }

}
