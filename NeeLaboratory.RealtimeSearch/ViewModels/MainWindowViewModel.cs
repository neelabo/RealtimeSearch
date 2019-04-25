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
        #region INotifyPropertyChanged Support

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value;
            this.RaisePropertyChanged(propertyName);
            return true;
        }

        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void AddPropertyChanged(string propertyName, PropertyChangedEventHandler handler)
        {
            PropertyChanged += (s, e) => { if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == propertyName) handler?.Invoke(s, e); };
        }

        #endregion

        private Models _models;
        private string _inputKeyword;
        private DelayValue<string> _keyword;
        private string _defaultWindowTitle;
        private History _history;
        private string _resultMessage;
        private bool _isRenaming;
        private Setting _setting;
        private string _settingFileName;
        private ClipboardSearch _clipboardSearch;


        public MainWindowViewModel()
        {
            _defaultWindowTitle = $"{App.Config.ProductName} {App.Config.ProductVersion}";
#if DEBUG
            _defaultWindowTitle += " [Debug]";
#endif

            _settingFileName = (App.Config.LocalApplicationDataPath) + "\\UserSetting.xml";

            _keyword = new DelayValue<string>("");
            _keyword.ValueChanged += async (s, e) => await SearchAsync(false);
        }


        public event EventHandler SearchResultChanged;



        public Models Models
        {
            get { return _models; }
            set { if (_models != value) { _models = value; RaisePropertyChanged(); } }
        }

        public string WindowTitle => _defaultWindowTitle;

        public string InputKeyword
        {
            get { return _inputKeyword; }
            set
            {
                if (SetProperty(ref _inputKeyword, value))
                {
                    SetKeywordDelay(_inputKeyword);
                }
            }
        }

        public History History
        {
            get { return _history; }
            set { if (_history != value) { _history = value; RaisePropertyChanged(); } }
        }

        public string ResultMessage
        {
            get { return _resultMessage; }
            set { if (_resultMessage != value) { _resultMessage = value; RaisePropertyChanged(); } }
        }

        public bool IsRenaming
        {
            get { return _isRenaming; }
            set { if (_isRenaming != value) { _isRenaming = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(IsTipsVisibled)); } }
        }

        public bool IsTipsVisibled
        {
            get { return !Setting.IsDetailVisibled && !IsRenaming; }
        }


        public Setting Setting
        {
            get { return _setting; }
            set { _setting = value; RaisePropertyChanged(); }
        }



        [System.Diagnostics.Conditional("DEBUG")]
        private void __Sleep(int ms)
        {
            System.Threading.Thread.Sleep(ms);
        }


        public void LoadSetting()
        {
            Setting = Setting.LoadOrDefault(_settingFileName);
            Setting.SearchAreas.CollectionChanged += SearchAreas_CollectionChanged;
            Setting.PropertyChanged += Setting_PropertyChanged;
        }

        public void Open(Window window)
        {
            if (Setting == null) throw new InvalidOperationException();

            Models = new Models(Setting);
            Models.SearchResultChanged += Models_SearchResultChanged;

            History = new History();

            _clipboardSearch = new ClipboardSearch(Setting);
            _clipboardSearch.ClipboardChanged += ClipboardSearch_ClipboardChanged;
            _clipboardSearch.Start(window);
        }

        public void Close()
        {
            _clipboardSearch.Stop();
            Setting.Save(_settingFileName);
        }


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
            InputKeyword = e.Keyword;
        }

        private void SearchAreas_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Models.ReIndex();
        }


        public void RestoreWindowPlacement(Window window)
        {
            if (Setting.WindowRect == Rect.Empty) return;

            Rect rect = Setting.WindowRect;
            window.Left = rect.Left;
            window.Top = rect.Top;
            window.Width = rect.Width;
            window.Height = rect.Height;
        }

        public void StoreWindowPlacement(Window window)
        {
            Setting.WindowRect = window.RestoreBounds;
        }

        // キーワード即時設定
        public void SetKeyword(string keyword)
        {
            _keyword.SetValue(keyword, 0, true);
        }

        // キーワード遅延設定
        public void SetKeywordDelay(string keyword)
        {
            _keyword.SetValue(keyword, 200);
        }

        public void SetClipboard(string text)
        {
            _clipboardSearch.SetClipboard(text);
        }

        public void AddHistory()
        {
            var keyword = Models.SearchResult?.Keyword;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                Debug.WriteLine($"AddHistory: {keyword}");
                History.Add(keyword);
            }
        }

        public void Rreflesh(string path)
        {
            Models.Reflesh(path);
        }

        public void Rename(string src, string dst)
        {
            Models.Rename(src, dst);
        }

        public async Task SearchAsync(bool isForce)
        {
            await Models.SearchAsync(_keyword.Value?.Trim(), isForce);
        }

        public void WebSearch()
        {
            //URLで使えない特殊文字。ひとまず変換なしで渡してみる
            //\　　'　　|　　`　　^　　"　　<　　>　　)　　(　　}　　{　　]　　[

            // キーワード整形。空白を"+"にする
            string query = _keyword.Value?.Trim();
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
        private RelayCommand _toggleDetailVisibleCommand;
        public RelayCommand ToggleDetailVisibleCommand
        {
            get { return _toggleDetailVisibleCommand = _toggleDetailVisibleCommand ?? new RelayCommand(ToggleDetailVisibleCommand_Executed); }
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
}
