using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Collections.ObjectModel;
using NeeLaboratory.IO.Search;

namespace NeeLaboratory.RealtimeSearch
{
    public class MainWindowViewModel : BindableBase
    {
        private Setting _setting;
        private Messenger _messenger;
        private Search _search;
        private string _inputKeyword = "";
        private DelayValue<string> _keyword;
        private string _defaultWindowTitle;
        private History _history;
        private string _resultMessage = "";
        private bool _isRenaming;
        private ClipboardSearch? _clipboardSearch;
        private FileIO _fileIO;
        private ExternalProgramCollection _programs;

        public MainWindowViewModel(Setting setting, Messenger messenger)
        {
            _setting = setting;
            _setting.PropertyChanged += Setting_PropertyChanged;

            _messenger = messenger;

            _defaultWindowTitle = App.Config.ProductName;

            _keyword = new DelayValue<string>("");
            _keyword.ValueChanged += async (s, e) => await SearchAsync(false);

            _search = new Search(setting);
            _search.SearchResultChanged += Models_SearchResultChanged;

            _history = new History();

            _fileIO = new FileIO();
            _fileIO.AddPropertyChanged(nameof(_fileIO.Error), FileIO_ErrorChanged);

            _programs = new ExternalProgramCollection(_setting);
            _programs.AddPropertyChanged(nameof(_programs.Error), Programs_ErrorChanged);
        }

        private void Programs_ErrorChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(_programs.Error)) return;

            ShowMessageBox(_programs.Error);
            _programs.ClearError();
        }

        private void FileIO_ErrorChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(_fileIO.Error)) return;

            ShowMessageBox(_fileIO.Error);
            _fileIO.ClearError();
        }

        private void ShowMessageBox(string message)
        {
            _messenger.Send(this, new ShowMessageBoxMessage(message));
        }

        public event EventHandler? SearchResultChanged;


        public Search Search
        {
            get { return _search; }
            set { SetProperty(ref _search, value); }
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

        public void StartClipboardWatch(Window window)
        {
            // クリップボード監視
            _clipboardSearch = new ClipboardSearch(Setting);
            _clipboardSearch.ClipboardChanged += ClipboardSearch_ClipboardChanged;
            _clipboardSearch.Start(window);
        }

        public void StopClipboardWatch()
        {
            _clipboardSearch?.Stop();
        }


        private void Models_SearchResultChanged(object? sender, EventArgs e)
        {
            SearchResultChanged?.Invoke(sender, EventArgs.Empty);

            if (_search.SearchResult != null && _search.SearchResult.Items.Count == 0)
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
            var keyword = _search.SearchResult?.Keyword;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                Debug.WriteLine($"AddHistory: {keyword}");
                History.Add(keyword);
            }
        }

        public void Rreflesh(string path)
        {
            _search.Reflesh(path);
        }

        // TODO: VMの責務ではない。MainWindowModel が欲しくなってきた
        public void Rename(NodeContent file, string newValue)
        {
            var invalidChar = _fileIO.CheckInvalidFileNameChars(newValue);
            if (invalidChar != '\0')
            {
                ShowMessageBox($"ファイル名に使用できない文字が含まれています。( {invalidChar} )");
                return;
            }

            var src = file.Path;
            var dst = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(src) ?? "", newValue);
            var newName = _fileIO.Rename(src, dst);
            if (newName is not null)
            {
                _search.Rename(src, newName);
            }
        }

        public async Task SearchAsync(bool isForce)
        {
            await _search.SearchAsync(_keyword.Value.Trim(), isForce);
        }

        // TODO: VMの責務ではない
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


        public void Execute(IEnumerable<NodeContent> files)
        {
            _programs.Execute(files);
        }

        public void Execute(IEnumerable<NodeContent> files, int programId)
        {
            _programs.Execute(files, programId);
        }

        public void ExecuteDefault(IEnumerable<NodeContent> files)
        {
            _programs.ExecuteDefault(files);
        }
    }
}
