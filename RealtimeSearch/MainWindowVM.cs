// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;

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

        private string _DefaultWindowTitle;

        public string WindowTitle
        {
            get
            {
                if (string.IsNullOrEmpty(SearchEngine.SearchKeyword))
                {
                    return _DefaultWindowTitle;
                }
                else
                {
                    return SearchEngine.SearchKeyword + " - " + _DefaultWindowTitle;
                }
            }
        }

        // 検索キーワード
        private string _Keyword;
        public string Keyword
        {
            get { return _Keyword; }
            set { _Keyword = value; OnPropertyChanged(); Search(); }
        }
  
        // ステータスバー
        private string _Information;
        public string Information
        {
            get { return _Information; }
            set { _Information = value; OnPropertyChanged(); }
        }

        // 検索エンジン
        public SearchEngine SearchEngine { get; private set; }

        // 検索結果
        public List<File> Files { get; private set; }

        // 検索コマンド
        public ICommand CommandSearch { get; private set; }

        // 設定
        #region Property: Setting
        private Setting _Setting;
        public Setting Setting
        {
            get { return _Setting; }
            set { _Setting = value; OnPropertyChanged(); }
        }
        #endregion

        // 設定ファイル名
        private string _SettingFileName;

        // クリップボード監視
        private ClipboardListner _ClipboardListner;

        // クリップボード監視フラグ
        public bool IsEnableClipboardListner { get; set; }


        [System.Diagnostics.Conditional("DEBUG")]
        private void __Sleep(int ms)
        {
            System.Threading.Thread.Sleep(ms);
        }


        public MainWindowVM()
        {
            SearchEngine = new SearchEngine();
            SearchEngine.Start();

            CommandSearch = new RelayCommand(Search);

            SearchEngine.ResultChanged += SearchEngine_ResultChanged;

            // title
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var ver = FileVersionInfo.GetVersionInfo(assembly.Location);
            _DefaultWindowTitle = $"{assembly.GetName().Name} {ver.FileMajorPart}.{ver.ProductMinorPart}";
#if DEBUG
            _DefaultWindowTitle += " [Debug]";
#endif

            // setting filename
            _SettingFileName = System.IO.Path.GetDirectoryName(assembly.Location) + "\\UserSetting.xml";
        }



        public void Open()
        {
            // 設定の読み込み
            if (System.IO.File.Exists(_SettingFileName))
            {
                Setting = Setting.Load(_SettingFileName);
                SearchEngine.IndexRequest(Setting.SearchPaths.ToArray()); // インデックス初期化
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
            _ClipboardListner = new ClipboardListner(window);
            _ClipboardListner.ClipboardUpdate += ClipboardListner_DrawClipboard;

            IsEnableClipboardListner = true;
        }


        private void SearchPaths_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SearchEngine.IndexRequest(Setting.SearchPaths.ToArray());
        }

        private string _CopyText;

        public void SetClipboard(string text)
        {
            System.Windows.Clipboard.SetDataObject(text);
            _CopyText = text;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetForegroundWindow();


        public async void ClipboardListner_DrawClipboard(object sender, Window window)
        {
            if (!IsEnableClipboardListner)
            {
                App.Log("cannot use clipboard: not enabled.");
                return;
            }

            IntPtr activeWindow = GetForegroundWindow();
            IntPtr thisWindow = new WindowInteropHelper(window).Handle;
            if (activeWindow == thisWindow)
            {
                App.Log("cannot use clipboard: window is active. (WIN32)");
                return;
            }

            // どうにも例外(CLIPBRD_E_CANT_OPEN)が発生してしまうのでリトライさせることにした
            for (int i = 0; i < 10; ++i)
            {
                try
                {
                    if (Setting.IsMonitorClipboard && Clipboard.ContainsText())
                    {
                        string text = Clipboard.GetText();
                        if (_CopyText == text) return; // コピーしたファイル名と同じであるなら処理しない

                        // クリップボードテキストの余計な空白を削除
                        var regex = new System.Text.RegularExpressions.Regex(@"\s+");
                        text = regex.Replace(text, " ").Trim();

                        // 即時検索
                        Keyword = text;
                        Search();
                    }
                    return;
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    Debug.WriteLine(ex.Message);
                    await Task.Delay(100);
                }
            }
            throw new ApplicationException("クリップボードの参照に失敗しました。");
        }


        public void Close()
        {
            // クリップボード監視終了
            _ClipboardListner.Dispose();

            // 設定の保存
            Setting.Save(_SettingFileName);
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
                Files = SearchEngine.SearchResult;
                Information = string.Format("{0:#,0} 個の項目", Files.Count);
            }

            OnPropertyChanged(nameof(Files));
            OnPropertyChanged(nameof(WindowTitle));
        }


        public void Search()
        {
            SearchEngine.SearchRequest(Keyword, Setting.IsSearchFolder);
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
    [ValueConversion(typeof(SearchEngineCommand), typeof(Visibility))]
    class CommandToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var cmd = (SearchEngineCommand)value;
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
