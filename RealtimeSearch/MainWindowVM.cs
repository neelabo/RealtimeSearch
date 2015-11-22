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
using System.Windows.Interop;
using System.Runtime.InteropServices;

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

#if DEBUG
        private string DefaultWindowTitle = "RealtimeSearch [Debug]";
#else
        private string DefaultWindowTitle = "RealtimeSearch";
#endif

        public string WindowTitle
        {
            get
            {
                if (string.IsNullOrEmpty(SearchEngine.CurrentKeyword))
                {
                    return DefaultWindowTitle;
                }
                else
                {
                    return SearchEngine.CurrentKeyword + " - " + DefaultWindowTitle;
                }
            }
        }

        //public FileList Files { get; set; }
        public List<File> Files { get; set; }

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
                Search(200);
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
            //Files = new FileList();

            SearchEngine = new SearchEngine();
            SearchEngine.Start();

            CommandSearch = new RelayCommand(Search);
            CommandReIndex = new RelayCommand(ReIndex);

            SearchEngine.ResultChanged += SearchEngine_ResultChanged;
        }


        //
        public void Open()
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
        }


        public void StartClipboardMonitor(Window window)
        { 
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


        public void RestoreWindowPlacement(Window window)
        {
            if (Setting.WindowPlacement == null) return;

            var placement = (WINDOWPLACEMENT)Setting.WindowPlacement;
            placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
            placement.flags = 0;
            placement.showCmd = (placement.showCmd == SW.SHOWMINIMIZED) ? SW.SHOWNORMAL : placement.showCmd;

            var hwnd = new WindowInteropHelper(window).Handle;
            NativeMethods.SetWindowPlacement(hwnd, ref placement);
        }


        public void StoreWindowPlacement(Window window)
        {
            WINDOWPLACEMENT placement;
            var hwnd = new WindowInteropHelper(window).Handle;
            NativeMethods.GetWindowPlacement(hwnd, out placement);

            Setting.WindowPlacement = placement;
        }






        private void SearchEngine_ResultChanged(object sender, int count)
        {
            if (count <= 0)
            {
                Files = new List<File>();
                Information = "";
            }
            else
            {
                Files = SearchEngine.Index.Matches;
                Information = string.Format("{0} 個の項目", Files.Count);
            }

            OnPropertyChanged(nameof(Files));
            OnPropertyChanged(nameof(WindowTitle));

#if false
            // 検索結果リスト作成
            this.Files.Clear();

            if (count <= 0) return;

            foreach (var match in SearchEngine.Index.matches)
            {
                this.Files.Add(match);
                //__Sleep(10);
            }

#endif
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
            Search(0);
        }

        // 遅延検索
        public void Search(int delay)
        {
            lock (lockObject)
            {
                delayTimer = delay;
                if (delayTask == null)
                {
                    delayTask = Task.Run(SearchAsync);
                }
            }
        }

        object lockObject = new object();

        Task delayTask;
        int delayTimer;

        //
        private async Task SearchAsync()
        {
            while (delayTimer > 0)
            {
                lock (lockObject)
                {
                    delayTimer -= 20;
                }
                await Task.Delay(20);
            }
            
            SearchEngine.SearchRequest(Keyword);

            lock (lockObject)
            {
                delayTask = null;
            }
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


    // コマンド状態を処理中表示に変換する
    [ValueConversion(typeof(MyCommand), typeof(Visibility))]
    class CommandToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var cmd = (MyCommand)value;
            return (cmd != null) ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 文字列状態を処理中表示に変換する
    [ValueConversion(typeof(string), typeof(Visibility))]
    class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var s = (string)value;
            return string.IsNullOrEmpty(s) ? Visibility.Hidden : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
