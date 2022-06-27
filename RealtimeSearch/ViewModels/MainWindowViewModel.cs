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
using System.Windows.Input;
using System.Windows.Interop;
using System.Collections.ObjectModel;

namespace NeeLaboratory.RealtimeSearch
{
    public class MainWindowViewModel : BindableBase
    {
        private Setting _setting;
        private Models _models;
        private string _inputKeyword = "";
        private DelayValue<string> _keyword;
        private string _defaultWindowTitle;
        private History _history;
        private string _resultMessage = "";
        private bool _isRenaming;
        private ClipboardSearch? _clipboardSearch;


        public MainWindowViewModel(Setting setting)
        {
            _setting = setting;
            _setting.PropertyChanged += Setting_PropertyChanged;

            _defaultWindowTitle = App.Config.ProductName;

            _keyword = new DelayValue<string>("");
            _keyword.ValueChanged += async (s, e) => await SearchAsync(false);

            _models = new Models(setting);
            _models.SearchResultChanged += Models_SearchResultChanged;

            _history = new History();
        }


        public event EventHandler? SearchResultChanged;


        public Models Models
        {
            get { return _models; }
            set { SetProperty(ref _models, value); }
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
            ////set { if (_history != value) { _history = value; RaisePropertyChanged(); } }
        }

        public string ResultMessage
        {
            get { return _resultMessage; }
            set { SetProperty(ref _resultMessage, value); }
        }

        public bool IsRenaming
        {
            get { return _isRenaming; }
            set
            {
                if (SetProperty(ref _isRenaming, value))
                {
                    RaisePropertyChanged(nameof(IsTipsVisibled));
                }
            }
        }

        public bool IsTipsVisibled
        {
            get { return !_setting.IsDetailVisibled && !IsRenaming; }
        }


        public Setting Setting
        {
            get { return _setting; }
        }



        [System.Diagnostics.Conditional("DEBUG")]
        private void __Sleep(int ms)
        {
            System.Threading.Thread.Sleep(ms);
        }

        public void Open(Window window)
        {
            // クリップボード監視
            _clipboardSearch = new ClipboardSearch(Setting);
            _clipboardSearch.ClipboardChanged += ClipboardSearch_ClipboardChanged;
            _clipboardSearch.Start(window);
        }

        public void Close()
        {
            _clipboardSearch?.Stop();
        }


        private void Models_SearchResultChanged(object? sender, EventArgs e)
        {
            SearchResultChanged?.Invoke(sender, EventArgs.Empty);

            if (_models.SearchResult != null && _models.SearchResult.Items.Count == 0)
            {
                ResultMessage = $"条件に一致する項目はありません。";
            }
            else
            {
                ResultMessage = "";
            }
        }

        private void Setting_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_setting.IsDetailVisibled))
            {
                RaisePropertyChanged(nameof(IsTipsVisibled));
            }
        }

        private void ClipboardSearch_ClipboardChanged(object? sender, ClipboardChangedEventArgs e)
        {
            InputKeyword = e.Keyword;

            SetKeyword(e.Keyword);
            AddHistory();
        }

        public void RestoreWindowPlacement(Window window)
        {
            if (_setting.WindowPlacement.HasValue)
            {
                WindowPlacement.SetPlacement(window, _setting.WindowPlacement);
            }
        }

        public void StoreWindowPlacement(Window window)
        {
            _setting.WindowPlacement = WindowPlacement.GetPlacement(window);
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
            _clipboardSearch?.SetClipboard(text);
        }

        public void AddHistory()
        {
            var keyword = _models.SearchResult?.Keyword;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                Debug.WriteLine($"AddHistory: {keyword}");
                History.Add(keyword);
            }
        }

        public void Rreflesh(string path)
        {
            _models.Reflesh(path);
        }

        public void Rename(string src, string dst)
        {
            _models.Rename(src, dst);
        }

        public async Task SearchAsync(bool isForce)
        {
            await _models.SearchAsync(_keyword.Value.Trim(), isForce);
        }

        public void WebSearch()
        {
            //URLで使えない特殊文字。ひとまず変換なしで渡してみる
            //\　　'　　|　　`　　^　　"　　<　　>　　)　　(　　}　　{　　]　　[

            // キーワード整形。空白を"+"にする
            string query = _keyword.Value.Trim();
            if (string.IsNullOrEmpty(query)) return;
            query = query.Replace("+", "%2B");
            query = Regex.Replace(query, @"\s+", "+");

            string url = _setting.WebSearchFormat.Replace("$(query)", query);
            Debug.WriteLine(url);

            var startInfo = new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true };
            System.Diagnostics.Process.Start(startInfo);
        }

        /// <summary>
        /// ToggleDetailVisibleCommand command.
        /// </summary>
        private RelayCommand? _toggleDetailVisibleCommand;
        public RelayCommand ToggleDetailVisibleCommand
        {
            get { return _toggleDetailVisibleCommand = _toggleDetailVisibleCommand ?? new RelayCommand(ToggleDetailVisibleCommand_Executed); }
        }

        private void ToggleDetailVisibleCommand_Executed()
        {
            _setting.IsDetailVisibled = !_setting.IsDetailVisibled;
        }
    }

}
