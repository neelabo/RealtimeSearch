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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Collections.ObjectModel;

namespace NeeLaboratory.RealtimeSearch
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(name));
            }
        }
        #endregion

        public event EventHandler SearchResultChanged;

        private string _defaultWindowTitle;
        public string WindowTitle => _defaultWindowTitle;

        // 検索キーワード
        private string _keyword = "";
        public string Keyword
        {
            get { return _keyword; }
            set { _keyword = value; RaisePropertyChanged(); var task = SearchAsync(); }
        }


        /// <summary>
        /// Models property.
        /// </summary>
        private Models _models;
        public Models Models
        {
            get { return _models; }
            set { if (_models != value) { _models = value; RaisePropertyChanged(); } }
        }


        /// <summary>
        /// History property.
        /// </summary>
        private History _history;
        public History History
        {
            get { return _history; }
            set { if (_history != value) { _history = value; RaisePropertyChanged(); } }
        }


        /// <summary>
        /// ResultMessage property.
        /// </summary>
        private string _ResultMessage;
        public string ResultMessage
        {
            get { return _ResultMessage; }
            set { if (_ResultMessage != value) { _ResultMessage = value; RaisePropertyChanged(); } }
        }


        /// <summary>
        /// IsRenaming property.
        /// </summary>
        private bool _IsRenaming;
        public bool IsRenaming
        {
            get { return _IsRenaming; }
            set { if (_IsRenaming != value) { _IsRenaming = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(IsTipsVisibled)); } }
        }


        /// <summary>
        /// IsTipsVisibled property.
        /// </summary>
        public bool IsTipsVisibled
        {
            get { return !Setting.IsDetailVisibled && !IsRenaming; }
        }




        // 設定 ... これも移動スべきか
        #region Property: Setting
        private Setting _setting;
        public Setting Setting
        {
            get { return _setting; }
            set { _setting = value; RaisePropertyChanged(); }
        }
        #endregion

        // 設定ファイル名
        private string _settingFileName;


        public ClipboardSearch ClipboardSearch;


        [System.Diagnostics.Conditional("DEBUG")]
        private void __Sleep(int ms)
        {
            System.Threading.Thread.Sleep(ms);
        }

        /// <summary>
        /// 
        /// </summary>
        public MainWindowViewModel()
        {
            // title
            _defaultWindowTitle = $"{App.Config.ProductName} {App.Config.ProductVersion}";
#if DEBUG
            _defaultWindowTitle += " [Debug]";
#endif

            // setting filename
            _settingFileName = (App.Config.LocalApplicationDataPath) + "\\UserSetting.xml";
        }

        /// <summary>
        /// Model PropertyChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Models_SearchResultChanged(object sender, EventArgs e)
        {
            SearchResultChanged?.Invoke(sender, null);

            if (Models.SearchResult.Items.Count == 0)
            {
                ResultMessage = $"条件に一致する項目はありません。";
            }
            else
            {
                ResultMessage = null;
            }

            History.Add(Models.SearchResult.Keyword);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="window"></param>
        public void Open(Window window)
        {
            // 設定読み込み
            Setting = Setting.LoadOrDefault(_settingFileName);
            Setting.SearchPaths.CollectionChanged += SearchPaths_CollectionChanged;
            Setting.PropertyChanged += Setting_PropertyChanged;

            // 初期化
            Models = new Models(Setting);
            Models.SearchResultChanged += Models_SearchResultChanged;

            //
            History = new History();

            ClipboardSearch = new ClipboardSearch(Setting);
            ClipboardSearch.ClipboardChanged += ClipboardSearch_ClipboardChanged;
            ClipboardSearch.Start(window);

            // Bindng Events
        }

        private void Setting_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Setting.IsDetailVisibled))
            {
                RaisePropertyChanged(nameof(IsTipsVisibled));
            }
        }

        private void ClipboardSearch_ClipboardChanged(object sender, ClipboardChangedEventArgs e)
        {
            Keyword = e.Keyword;
        }

        private void SearchPaths_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Models.ReIndex();
        }

        public void Close()
        {
            // クリップボード監視終了
            ClipboardSearch.Stop();

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



        /// <summary>
        /// 検索
        /// </summary>
        /// <returns></returns>
        public async Task SearchAsync()
        {
            var keyword = new Regex(@"\s+").Replace(this.Keyword, " ").Trim();

            await Models.SearchAsync(keyword);
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

        /// <summary>
        /// ToggleDetailVisibleCommand command.
        /// </summary>
        private RelayCommand _ToggleDetailVisibleCommand;
        public RelayCommand ToggleDetailVisibleCommand
        {
            get { return _ToggleDetailVisibleCommand = _ToggleDetailVisibleCommand ?? new RelayCommand(ToggleDetailVisibleCommand_Executed); }
        }

        private void ToggleDetailVisibleCommand_Executed()
        {
            Setting.IsDetailVisibled = !Setting.IsDetailVisibled;
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


    // NULLの場合、非表示にする
    [ValueConversion(typeof(object), typeof(Visibility))]
    internal class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
