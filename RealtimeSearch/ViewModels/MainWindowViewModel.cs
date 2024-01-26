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
using System.Linq;
using NeeLaboratory.IO.Search.Files;
using NeeLaboratory.ComponentModel;
using NeeLaboratory.Windows.Input;

namespace NeeLaboratory.RealtimeSearch
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly AppConfig _appConfig;
        private readonly Messenger _messenger;
        private Search _search;
        private readonly WebSearch _webSearch;
        private string _inputKeyword = "";
        private readonly DelayValue<string> _keyword;
        private readonly string _defaultWindowTitle;
        private readonly History _history;
        private string _resultMessage = "";
        private bool _isRenaming;
        private ClipboardSearch? _clipboardSearch;
        private readonly ExternalProgramCollection _programs;


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
            get { return !_appConfig.IsDetailVisible && !IsRenaming; }
        }

        public bool IsDetailVisibled
        {
            get { return _appConfig.IsDetailVisible; }
            set { _appConfig.IsDetailVisible = value; }
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
            get { return _appConfig.AllowFolder; }
            set { _appConfig.AllowFolder = value; }
        }
        


        [Conditional("DEBUG")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:命名スタイル", Justification = "<保留中>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:使用されていないプライベート メンバーを削除する", Justification = "<保留中>")]
        private static void __Sleep(int ms)
        {
            System.Threading.Thread.Sleep(ms);
        }


        private void Programs_ErrorChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(_programs.Error)) return;

            ShowMessageBox(_programs.Error);
            _programs.ClearError();
        }

        public void Loaded()
        {
            // 検索パスが設定されていなければ設定画面を開く
            if (_appConfig.SearchAreas.Count <= 0)
            {
                _messenger.Send(this, new ShowSettingWindowMessage());
            }

            _search.ReIndex();
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
                case nameof(_appConfig.IsDetailVisible):
                    RaisePropertyChanged(nameof(IsDetailVisibled));
                    RaisePropertyChanged(nameof(IsTipsVisibled));
                    break;

                case nameof(_appConfig.IsTopmost):
                    RaisePropertyChanged(nameof(IsTopmost));
                    break;

                case nameof(_appConfig.AllowFolder):
                    RaisePropertyChanged(nameof(AllowFolder));
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
            WindowPlacementTools.RestoreWindowPlacement(window, _appConfig.WindowPlacement);
        }

        public void StoreWindowPlacement(Window window)
        {
            _appConfig.WindowPlacement = WindowPlacementTools.StoreWindowPlacement(window, true);
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

        public void CopyFilesToClipboard(List<FileItem> files)
        {
            ClipboardTools.SetFileDropList(files.Select(e => e.Path).ToArray());
        }

        public void CopyNameToClipboard(FileItem file)
        {
            var text = file.IsDirectory
                ? System.IO.Path.GetFileName(file.Path)
                : System.IO.Path.GetFileNameWithoutExtension(file.Path);
            ClipboardTools.SetText(text);
        }


        public void AddHistory()
        {
            var keyword = _search.SearchResult?.Keyword ?? "";
            History.Add(keyword);
        }

        public void Refresh(string path)
        {
            _search.Refresh(path);
        }

        public void OpenProperty(FileItem item)
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

        public void Delete(IList<FileItem> items)
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

        public void Rename(FileItem file, string newValue)
        {
            var folder = System.IO.Path.GetDirectoryName(file.Path) ?? "";

            var src = file.Path;
            var dst = System.IO.Path.Combine(folder, newValue);

            // directory merge?
            if (file.IsDirectory && System.IO.Directory.Exists(dst))
            {
                var msg = $"この宛先には既に '{newValue}' フォルダーが存在します。\n\n同じ名前のファイルがある場合、かっこで囲まれた番号が付加され、区別されます。\n\nフォルダーを統合しますか？";
                var messageBox = new ShowMessageBoxMessage(msg, "フォルダーの上書き確認", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                _messenger.Send(this, messageBox);

                if (messageBox.Result == MessageBoxResult.OK)
                {
                    try
                    {
                        FileSystem.MergeDirectory(src, dst);
                    }
                    catch (Exception ex)
                    {
                        _messenger.Send(this, new ShowMessageBoxMessage($"フォルダーの統合に失敗しました\n\n原因: {ex.Message}", MessageBoxImage.Error));
                    }
                }
            }
            else
            {
                try
                {
                    FileSystem.Rename(src, dst);
                }
                catch (Exception ex)
                {
                    _messenger.Send(this, new ShowMessageBoxMessage($"名前の変更に失敗しました\n\n原因: {ex.Message}", MessageBoxImage.Error));
                }
            }
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


        public void Execute(IEnumerable<FileItem> files)
        {
            _programs.Execute(files);
        }

        public void Execute(IEnumerable<FileItem> files, int programId)
        {
            _programs.Execute(files, programId);
        }

        public void ExecuteDefault(IEnumerable<FileItem> files)
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
            get { return _toggleDetailVisibleCommand ??= new RelayCommand(ToggleDetailVisibleCommand_Executed); }
        }

        private void ToggleDetailVisibleCommand_Executed()
        {
            _appConfig.IsDetailVisible = !_appConfig.IsDetailVisible;
        }

        #endregion Commands

    }
}
