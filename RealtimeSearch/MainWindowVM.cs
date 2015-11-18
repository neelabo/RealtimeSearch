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
    public class MainWindowVM : INotifyPropertyChanged
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

        public FileList Files { get; set; }

        public ICommand CommandSearch { get; set; }
        public ICommand CommandReIndex { get; set; }
    

#if false
        volatile bool _SearchLock;
        public bool SearchLock
        {
            get { return _SearchLock; }
            set { _SearchLock = value; NotifyPropertyChanged("SearchLock"); }
        }
      
        private bool keywordDarty;
#endif

        private string _Keyword;
        public string Keyword
        {
            get { return _Keyword; }
            set
            {
                var regex = new System.Text.RegularExpressions.Regex(@"\s+");
                string newKeyword = regex.Replace(value, " ");
                //if (keyword != newKeyword) keywordDarty = true;
                _Keyword = newKeyword;
                OnPropertyChanged();
            }
        }

#if false
        private string searchKeyword;
        public string SearchKeyword
        {
            get { return searchKeyword; }
            set { searchKeyword = value; OnPropertyChanged(); }
        }
#endif

        // メッセージだね
        private string _Information;
        public string Information
        {
            get { return _Information; }
            set { _Information = value; OnPropertyChanged(); }
        }

        //private Index index;
        public SearchEngine SearchEngine { get; set; }

        //
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
                //index = new Index(ConfigViewModel.IndexPaths);
                //SearchEngine = new SearchEngine();
                //SearchEngine.IndexRequest(ConfigViewModel.IndexPaths.ToArray());
            }
        }



        [System.Diagnostics.Conditional("DEBUG")]
        private void __Sleep(int ms)
        {
            System.Threading.Thread.Sleep(ms);
        }


        //
        public MainWindowVM()
        {
            Files = new FileList();

            SearchEngine = new SearchEngine();
            SearchEngine.Start();

            ConfigViewModel = new ConfigViewModel();

            CommandSearch = new RelayCommand(Search);
            CommandReIndex = new RelayCommand(ReIndex);

            SearchEngine.ResultChanged += SearchEngine_ResultChanged;
        }


        private void SearchEngine_ResultChanged(object sender, EventArgs e)
        {
            // 検索結果リスト作成
            this.Files.Clear();
            foreach (var match in SearchEngine.Index.matches)
            {
                this.Files.Add(match);
                //__Sleep(10);
            }

            Information = string.Format("{0} 個の項目", SearchEngine.Index.matches.Count);
        }


#if false
        //
        public void StartSearchEngine()
        {
            //SearchEngine.Start();
        }
#endif


        //
        public void UpdateConfig()
        {
            if (ConfigViewModel.IsDarty)
            {
                ConfigViewModel.IsDarty = false;
                //index = new Index(ConfigViewModel.IndexPaths);
                SearchEngine.IndexRequest(ConfigViewModel.IndexPaths.ToArray());
            }
        }

        //
        public void ReIndex()
        {
            SearchEngine.ReIndexRequest();
        }
#if false
        // インデックス作成
        // Note: SearchEngine版では不要？
        public void GenerateIndex()
        {
            //SearchLock = true;

            // TODO: データベース初期化
            Information = "初期化中";

            //fileDatabase = new FileDatabase();
            //index.Initialize(); 

            //__Sleep(5000);

            Information = "";

            //SearchLock = false;
        }
#endif

#if false
        // えーとねえ。
        public bool CanSearch()
        {
            //return (!string.IsNullOrEmpty(Keyword) && keywordDarty && !SearchLock);
            return true;
        }
#endif

#if false
        public void SetKeyworkdDarty()
        {
            keywordDarty = true;
        }
#endif

        // 検索
        // Note: SearchEngineではこの構造自体変更が必要
        // Note: SearchEngine.Index.matchesの直接アクセスはダメです
        public void Search()
        {
            //SearchLock = true;

            //SearchKeyword = Keyword;

            //Information = "検索中...";
            //keywordDarty = false;

            // 検索
            //index.Check(Keyword);
            SearchEngine.SearchRequest(Keyword);

#if false
            // 待たないとねえ,,

            // 検索結果リスト作成
            this.Files.Clear();
            foreach (var match in SearchEngine.Index.matches)
            {
                this.Files.Add(match);

                __Sleep(10);
            }

            Status = string.Format("{0} 個の項目", SearchEngine.Index.matches.Count);
#endif

            //SearchLock = false;
        }
    }

    // ファイルサイズを表示用に整形する
    [ValueConversion(typeof(long), typeof(string))]
    class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            long size = (long)value;
            return string.Format("{0:#,0} KB", (size + 1024 - 1) / 1024);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
