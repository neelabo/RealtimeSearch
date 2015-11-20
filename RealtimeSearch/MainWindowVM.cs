using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.ComponentModel;
using System.Diagnostics;

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
    

        private string _Keyword;
        public string Keyword
        {
            get { return _Keyword; }
            set
            {
                var regex = new System.Text.RegularExpressions.Regex(@"\s+");
                string newKeyword = regex.Replace(value, " ");
                _Keyword = newKeyword;
                OnPropertyChanged();
            }
        }
  
        // メッセージだね
        private string _Information;
        public string Information
        {
            get { return _Information; }
            set { _Information = value; OnPropertyChanged(); }
        }

        //private Index index;
        public SearchEngine SearchEngine { get; set; }

        #region Property: Setting
        private Setting _Setting;
        public Setting Setting
        {
            get { return _Setting; }
            set { _Setting = value; OnPropertyChanged(); }
        }
        #endregion

        private ClipboardListner ClipboardListner;

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

            CommandSearch = new RelayCommand(Search);
            CommandReIndex = new RelayCommand(ReIndex);

            SearchEngine.ResultChanged += SearchEngine_ResultChanged;
        }


        //
        public void Open(Window window)
        {
            // 設定の読み込み
            System.Reflection.Assembly myAssembly = System.Reflection.Assembly.GetEntryAssembly();
            string defaultConfigPath = System.IO.Path.GetDirectoryName(myAssembly.Location) + "\\UserSetting.xml";

            if (System.IO.File.Exists(defaultConfigPath))
            {
                Setting = Setting.Load(defaultConfigPath);
                UpdateSetting();
            }
            else
            {
                Setting = new Setting();
            }

            // Bindng Events
            Setting.SearchPaths.CollectionChanged += SearchPaths_CollectionChanged;

            // クリップボード監視
            ClipboardListner = new ClipboardListner(window);
            ClipboardListner.ClipboardUpdate += ClipboardListner_DrawClipboard;
        }


        private void SearchPaths_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SearchEngine.IndexRequest(Setting.SearchPaths.ToArray());
        }


        public async void ClipboardListner_DrawClipboard(object sender, System.EventArgs e)
        {
            // どうにも例外(CLIPBRD_E_CANT_OPEN)が発生してしまうのでリトライさせることにした
            RETRY:
            try
            {
                if (Setting.IsMonitorClipboard && Clipboard.ContainsText())
                {
                    // キーワード設定
                    Keyword = Clipboard.GetText();

                    // 検索
                    Search();
                }
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                Debug.WriteLine(ex.Message);

                await Task.Delay(100);
                goto RETRY;
            }
        }


        //
        public void Close()
        {
            // クリップボード監視終了
            ClipboardListner.Dispose();

            // 設定の保存
            System.Reflection.Assembly myAssembly = System.Reflection.Assembly.GetEntryAssembly();
            string defaultConfigPath = System.IO.Path.GetDirectoryName(myAssembly.Location) + "\\UserSetting.xml";
            Setting.Save(defaultConfigPath);
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

        // ##
        public void UpdateSetting()
        {
            if (Setting.SearchPaths != null)
            {
                SearchEngine.IndexRequest(Setting.SearchPaths.ToArray());
            }
        }

        // インデックス再構築
        public void ReIndex()
        {
            SearchEngine.ReIndexRequest();
        }

        // 検索
        public void Search()
        {
            SearchEngine.SearchRequest(Keyword);
        }
    }


    // ファイルサイズを表示用に整形する
    [ValueConversion(typeof(long), typeof(string))]
    class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            long size = (long)value;
            return (size >= 0) ? string.Format("{0:#,0} KB", (size + 1024 - 1) / 1024) : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
