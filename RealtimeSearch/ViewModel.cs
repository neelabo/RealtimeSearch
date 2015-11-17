using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.ComponentModel;


namespace RealtimeSearch
{
    public class ViewModel : INotifyPropertyChanged
    {
        public FileList Files { get; set; }


        volatile bool _SearchLock;
        public bool SearchLock
        {
            get { return _SearchLock; }
            set { _SearchLock = value; NotifyPropertyChanged("SearchLock"); }
        }
      
        private bool keywordDarty;
        private string keyword;
        public string Keyword
        {
            get { return keyword; }
            set
            {
                var regex = new System.Text.RegularExpressions.Regex(@"\s+");
                string newKeyword = regex.Replace(value, " ");
                if (keyword != newKeyword) keywordDarty = true;
                keyword = newKeyword;
                NotifyPropertyChanged("Keyword");
            }
        }

        private string searchKeyword;
        public string SearchKeyword
        {
            get { return searchKeyword; }
            set { searchKeyword = value; NotifyPropertyChanged("SearchKeyword"); }
        }

        //
        private string status;
        public string Status
        {
            get { return status; }
            set { status = value; NotifyPropertyChanged("Status"); }
        }

        private Index index;

        private ConfigViewModel configViewModel;
        public ConfigViewModel ConfigViewModel
        {
            get
            {
                return configViewModel;
            }
            set
            {
                configViewModel = value;
                index = new Index(ConfigViewModel.IndexPaths);
            }
        }

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


        [System.Diagnostics.Conditional("DEBUG")]
        private void __Sleep(int ms)
        {
            System.Threading.Thread.Sleep(ms);
        }


        //
        public ViewModel()
        {
            Files = new FileList();

            ConfigViewModel = new ConfigViewModel();

            //fileDatabase = new FileDatabase(ConfigViewModel.IndexPaths);
            //if (Clipboard.ContainsText()) keyword = Clipboard.GetText();
        }

        //
        public void UpdateConfig()
        {
            if (ConfigViewModel.IsDarty)
            {
                ConfigViewModel.IsDarty = false;
                index = new Index(ConfigViewModel.IndexPaths);
            }
        }

        // インデックス作成
        public void GenerateIndex()
        {
            SearchLock = true;

            // TODO: データベース初期化
            Status = "初期化中";

            //fileDatabase = new FileDatabase();
            index.Initialize();

            __Sleep(5000);

            Status = "";

            SearchLock = false;
        }

        // えーとねえ。
        public bool CanSearch()
        {
            return (!string.IsNullOrEmpty(Keyword) && keywordDarty && !SearchLock);
        }

        public void SetKeyworkdDarty()
        {
            keywordDarty = true;
        }


        // 検索
        public void Search()
        {
            SearchLock = true;

            SearchKeyword = Keyword;

            Status = "検索中...";
            keywordDarty = false;

            // 検索
            index.Check(Keyword);

            // 検索結果リスト作成
            this.Files.Clear();
            foreach (var match in index.matches)
            {
                this.Files.Add(match);

                __Sleep(10);
            }

            Status = string.Format("{0} 個の項目", index.matches.Count);

            SearchLock = false;
        }
    }

    // 
    [ValueConversion(typeof(long), typeof(string))]
    class FileSizeConverter : IValueConverter
    {
        #region IValueConverter メンバ

        //
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            long size = (long)value;
            return string.Format("{0:#,0} KB", (size + 1024 - 1) / 1024);
        }

        //
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
