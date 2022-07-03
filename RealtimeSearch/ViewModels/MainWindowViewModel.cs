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
using System.Linq;

namespace NeeLaboratory.RealtimeSearch
{
    public class MainWindowViewModel : BindableBase
    {
        private AppConfig _appConfig;
        private Messenger _messenger;
        private Search _search;
        private WebSearch _webSearch;
        private string _inputKeyword = "";
        private DelayValue<string> _keyword;
        private string _defaultWindowTitle;
        private History _history;
        private string _resultMessage = "";
        private bool _isRenaming;
        private ClipboardSearch? _clipboardSearch;
        private FileRename _fileRename;
        private ExternalProgramCollection _programs;


        public MainWindowViewModel(AppConfig appConfig, Messenger messenger)
        {
            _appConfig = appConfig;
            _appConfig.PropertyChanged += Setting_PropertyChanged;

            _messenger = messenger;

            _defaultWindowTitle = App.AppInfo.ProductName;

            _keyword = new DelayValue<string>("");
            _keyword.ValueChanged += async (s, e) => await SearchAsync(false);

            _search = new Search(appConfig);
            _search.SearchResultChanged += Search_SearchResultChanged;

            _webSearch = new WebSearch(appConfig);

            _history = new History();

            _fileRename = new FileRename();
            _fileRename.Renamed += (s, e) => _search.Rename(e.OldFullPath, e.FullPath);
            _fileRename.AddPropertyChanged(nameof(_fileRename.Error), FileIO_ErrorChanged);

            _programs = new ExternalProgramCollection(_appConfig);
            _programs.AddPropertyChanged(nameof(_programs.Error), Programs_ErrorChanged);
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
            get { return !_appConfig.IsDetailVisibled && !IsRenaming; }
        }

        public bool IsDetailVisibled
        {
            get { return _appConfig.IsDetailVisibled; }
            set { _appConfig.IsDetailVisibled = value; }
        }

        public bool IsTopmost
        {
            get { return _appConfig.IsTopmost; }
            set
            {
                if (_appConfig.IsTopmost != value)
                {
                    _appConfig.IsTopmost = value;
                    RaisePropertyChanged(nameof(IsTopmost));
                }
            }
        }

        public bool AllowFolder
        {
            get { return _appConfig.SearchOption.AllowFolder; }
            set { _appConfig.SearchOption.AllowFolder = value; }
        }



        [System.Diagnostics.Conditional("DEBUG")]
        private void __Sleep(int ms)
        {
            System.Threading.Thread.Sleep(ms);
        }


        private void Programs_ErrorChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(_programs.Error)) return;

            ShowMessageBox(_programs.Error);
            _programs.ClearError();
        }

        private void FileIO_ErrorChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(_fileRename.Error)) return;

            ShowMessageBox(_fileRename.Error);
            _fileRename.ClearError();
        }

        public void Loaded()
        {
            // 検索パスが設定されていなければ設定画面を開く
            if (_appConfig.SearchAreas.Count <= 0)
            {
                _messenger.Send(this, new ShowSettingWindowMessage());
            }
        }

        private void ShowMessageBox(string message)
        {
            _messenger.Send(this, new ShowMessageBoxMessage(message));
        }

        public void StartClipboardWatch(Window window)
        {
            // クリップボード監視
            _clipboardSearch = new ClipboardSearch(_appConfig);
            _clipboardSearch.ClipboardChanged += ClipboardSearch_ClipboardChanged;
            _clipboardSearch.Start(window);
        }

        public void StopClipboardWatch()
        {
            _clipboardSearch?.Stop();
        }


        private void Search_SearchResultChanged(object? sender, EventArgs e)
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
            switch (e.PropertyName)
            {
                case nameof(_appConfig.IsDetailVisibled):
                    RaisePropertyChanged(nameof(IsDetailVisibled));
                    RaisePropertyChanged(nameof(IsTipsVisibled));
                    break;

                case nameof(_appConfig.IsTopmost):
                    RaisePropertyChanged(nameof(IsTopmost));
                    break;
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
            if (_appConfig.WindowPlacement.HasValue)
            {
                WindowPlacement.SetPlacement(window, _appConfig.WindowPlacement);
            }
        }

        public void StoreWindowPlacement(Window window)
        {
            _appConfig.WindowPlacement = WindowPlacement.GetPlacement(window);
        }

        public void StoreListViewCondition(List<ListViewColumnMemento> listViewColumnMementos)
        {
            _appConfig.ListViewColumnMemento = listViewColumnMementos;
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


        public void CopyFilesToClipboard(List<NodeContent> files)
        {
            ClipboardTools.SetFileDropList(files.Select(e => e.Path).ToArray());
        }

        public void CopyNameToClipboard(NodeContent file)
        {
            ClipboardTools.SetText(System.IO.Path.GetFileNameWithoutExtension(file.Path));
        }


        public void AddHistory()
        {
            var keyword = _search.SearchResult?.Keyword ?? "";
            History.Add(keyword);
        }

        public void Rreflesh(string path)
        {
            _search.Reflesh(path);
        }

        public void OpenProperty(NodeContent item)
        {
            try
            {
                ShellFileResource.OpenProperty(Application.Current.MainWindow, item.Path);
            }
            catch (Exception ex)
            {
                _messenger.Send(this, new ShowMessageBoxMessage(ex.Message, MessageBoxImage.Error));
            }
        }

        public void Delete(IList<NodeContent> items)
        {
            if (items.Count == 0) return;

            var text = (items.Count == 1)
                ? $"このファイルをごみ箱に移動しますか？\n\n{items[0].Path}"
                : $"これらの {items.Count} 個の項目をごみ箱に移しますか？";
            var dialogMessage = new ShowMessageBoxMessage(text, null, MessageBoxButton.OKCancel, MessageBoxImage.Question);
            _messenger.Send(this, dialogMessage);

            if (dialogMessage.Result == MessageBoxResult.OK)
            {
                try
                {
                    FileSystem.SendToRecycleBin(items.Select(e => e.Path).ToList());
                }
                catch (Exception ex)
                {
                    _messenger.Send(this, new ShowMessageBoxMessage($"ファイル削除に失敗しました\n\n原因: {ex.Message}", MessageBoxImage.Error));
                }
            }
        }


        public void Rename(NodeContent file, string newValue)
        {
            var folder = System.IO.Path.GetDirectoryName(file.Path) ?? "";
            var oldValue = System.IO.Path.GetFileName(file.Path);
            _fileRename.Rename(folder, oldValue, newValue);
        }


        public async Task ToggleAllowFolderAsync()
        {
            AllowFolder = !AllowFolder;
            RaisePropertyChanged(nameof(AllowFolder));

            await SearchAsync(true);
        }


        public async Task SearchAsync(bool isForce)
        {
            await _search.SearchAsync(_keyword.Value.Trim(), isForce);
        }


        public void WebSearch()
        {
            _webSearch.Search(_keyword.Value);
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


        // TODO: この存在の是非はWinUI3移行時に判定する
#region Commands

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
            _appConfig.IsDetailVisibled = !_appConfig.IsDetailVisibled;
        }

#endregion Commands

    }
}
