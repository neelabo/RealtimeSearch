﻿// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Collections.ObjectModel;

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

        public event EventHandler<SearchEngineState> StateMessageChanged;

        private string _defaultWindowTitle;
        public string WindowTitle => _defaultWindowTitle;

        // 検索キーワード
        private string _keyword = "";
        public string Keyword
        {
            get { return _keyword; }
            set { _keyword = value; OnPropertyChanged(); Search(false); }
        }

        // 検索履歴
        public ObservableCollection<string> History { get; set; }


        // ステータスバー
        private string _information = "";
        public string Information
        {
            get { return _information; }
            set { _information = value; OnPropertyChanged(); }
        }

        #region Property: IndexInformation
        private string _indexInformation = "";
        public string IndexInformation
        {
            get { return _indexInformation; }
            set { _indexInformation = value; OnPropertyChanged(); }
        }
        #endregion


        // 検索エンジン
        public SearchEngine SearchEngine { get; private set; }

        // 検索結果
        public ObservableCollection<NodeContent> Files { get; private set; }

        // 設定
        #region Property: Setting
        private Setting _setting;
        public Setting Setting
        {
            get { return _setting; }
            set { _setting = value; OnPropertyChanged(); }
        }
        #endregion

        // 設定ファイル名
        private string _settingFileName;

        // クリップボード監視
        private ClipboardListner _clipboardListner;

        // クリップボード監視フラグ
        public bool IsEnableClipboardListner { get; set; }


        [System.Diagnostics.Conditional("DEBUG")]
        private void __Sleep(int ms)
        {
            System.Threading.Thread.Sleep(ms);
        }


        public MainWindowVM()
        {
            FileInfo.InitializeDefaultResource();

            History = new ObservableCollection<string>();

            SearchEngine = new SearchEngine();
            SearchEngine.Start();

            SearchEngine.ResultChanged += (s, e) => SearchEngine_ResultChanged(s);
            SearchEngine.StateMessageChanged += StateMessageChanged;
            SearchEngine.IndexCountChanged += SearchEngine_IndexCountChanged;

            // title
            _defaultWindowTitle = $"{App.Config.ProductName} {App.Config.ProductVersion}";
#if DEBUG
            _defaultWindowTitle += " [Debug]";
#endif

            // setting filename
            _settingFileName = (App.Config.LocalApplicationDataPath) + "\\UserSetting.xml";
        }



        public void Open()
        {
            // 設定の読み込み
            if (System.IO.File.Exists(_settingFileName))
            {
                Setting = Setting.Load(_settingFileName);
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
            _clipboardListner = new ClipboardListner(window);
            _clipboardListner.ClipboardUpdate += ClipboardListner_DrawClipboard;

            IsEnableClipboardListner = true;
        }


        private void SearchPaths_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SearchEngine.IndexRequest(Setting.SearchPaths.ToArray());
        }

        private string _copyText;

        public void SetClipboard(string text)
        {
            System.Windows.Clipboard.SetDataObject(text);
            _copyText = text;
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
                        if (_copyText == text) return; // コピーしたファイル名と同じであるなら処理しない

                        // 即時検索
                        Keyword = new Regex(@"\s+").Replace(text, " ").Trim();
                        AddHistory(Keyword);
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
            _clipboardListner.Dispose();

            // 設定の保存
            Setting.Save(_settingFileName);
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

        // 情報通知
        private void SearchEngine_IndexCountChanged(object sender, string message)
        {
            if (SearchEngine.State == SearchEngineState.Search)
            {
                IndexInformation = message;
            }
            else
            {
                IndexInformation = "";
            }
        }

        // 結果変更
        private void SearchEngine_ResultChanged(object sender)
        {
            if (Files != SearchEngine.SearchResult)
            {
                Files = SearchEngine.SearchResult;
                OnPropertyChanged(nameof(Files));
                OnPropertyChanged(nameof(WindowTitle));
            }

            if (SearchEngine.SearchResult.Count <= 0)
            {
                Information = "";
            }
            else
            {
                Information = string.Format("{0:#,0} 個の項目", Files.Count);
            }
        }



        //
        public void Search(bool isAddHistory)
        {
            var keyword = new Regex(@"\s+").Replace(Keyword, " ").Trim();
            SearchEngine.SearchRequest(keyword, Setting.IsSearchFolder);

            if (isAddHistory) AddHistory(keyword);
        }

        //
        public void AddHistory(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return;

            if (this.History.Count <= 0)
            {
                this.History.Add(keyword);
            }
            else if (History.First() != keyword)
            {
                int index = History.IndexOf(keyword);
                if (index > 0)
                {
                    History.Move(index, 0);
                }
                else
                {
                    this.History.Insert(0, keyword);
                }
            }

            while (History.Count > 6)
            {
                History.RemoveAt(History.Count - 1);
            }
        }


        //
        public void WebSearch()
        {
            //URLで使えない特殊文字。ひとまず変換なしで渡してみる
            //\　　'　　|　　`　　^　　"　　<　　>　　)　　(　　}　　{　　]　　[

            // キーワード整形。空白を"+"にする
            string query = Keyword?.Trim();
            if (string.IsNullOrEmpty(query)) return;
            query = query.Replace("+", "%2B");
            query = Regex.Replace(query, @"\s+", "+");

            string url = this.Setting.WebSearchFormat.Replace("$(query)", query);
            Debug.WriteLine(url);
            System.Diagnostics.Process.Start(url);
        }
    }


    // ファイルサイズを表示用に整形する
    [ValueConversion(typeof(long), typeof(string))]
    internal class FileSizeConverter : IValueConverter
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
    internal class CommandToVisibilityConverter : IValueConverter
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
    internal class StringToVisibilityConverter : IValueConverter
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
