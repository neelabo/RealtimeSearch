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
using CommunityToolkit.Mvvm.Input;
using NeeLaboratory.IO.Search.Files;
using NeeLaboratory.Windows.Input;
using NeeLaboratory.RealtimeSearch.Models;
using NeeLaboratory.RealtimeSearch.Windows;
using NeeLaboratory.RealtimeSearch.Services;
using NeeLaboratory.RealtimeSearch.TextResource;
using NeeLaboratory.Windows.IO;
using NeeLaboratory.Messaging;
using NeeLaboratory.RealtimeSearch.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NeeLaboratory.RealtimeSearch.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly MainModel _model;
        private readonly AppSettings _settings;
        private readonly Messenger _messenger;
        private readonly string _defaultWindowTitle;
        private bool _isRenaming;
        private readonly ExternalProgramCollection _programs;
        private FileContent? _selectedItem;
        private IntPtr _hWnd;


        public MainWindowViewModel(AppSettings settings, Messenger messenger)
        {
            _settings = settings;
            _settings.PropertyChanged += Setting_PropertyChanged;

            _messenger = messenger;

            _model = new MainModel(_settings);
            _model.PropertyChanged += Model_PropertyChanged;

            _programs = new ExternalProgramCollection(_settings);
            _programs.SubscribePropertyChanged(nameof(_programs.Error), Programs_ErrorChanged);

            _defaultWindowTitle = ApplicationInfo.Current.ProductName;
        }


        public event EventHandler? SearchResultChanged
        {
            add { _model.SearchResultChanged += value; }
            remove { _model.SearchResultChanged -= value; }
        }


        public Search Search => _model.Search;


        public FileContent? SelectedItem
        {
            get { return _selectedItem; }
            set { SetProperty(ref _selectedItem, value); }
        }


        public string WindowTitle => _defaultWindowTitle;

        public string InputKeyword
        {
            get => _model.InputKeyword;
            set => _model.InputKeyword = value;
        }

        public History History => _model.History;

        public string ResultMessage
        {
            get => _model.ResultMessage;
            set => _model.ResultMessage = value;
        }

        public string CountMessage
        {
            get => _model.CountMessage;
            set => _model.CountMessage = value;
        }

        public bool IsRenaming
        {
            get { return _isRenaming; }
            set
            {
                if (SetProperty(ref _isRenaming, value))
                {
                    OnPropertyChanged(nameof(IsTipsVisible));
                }
            }
        }

        public bool IsTipsVisible
        {
            get { return !_settings.IsDetailVisible && !IsRenaming; }
        }

        public bool IsDetailVisible
        {
            get { return _settings.IsDetailVisible; }
            set { _settings.IsDetailVisible = value; }
        }

        public bool IsTopmost
        {
            get { return _settings.IsTopmost; }
            set
            {
                if (_settings.IsTopmost != value)
                {
                    _settings.IsTopmost = value;
                    OnPropertyChanged(nameof(IsTopmost));
                }
            }
        }

        public bool AllowFolder
        {
            get { return _settings.AllowFolder; }
            set { _settings.AllowFolder = value; }
        }

        public ExternalProgramCollection Programs => _programs;


        [Conditional("DEBUG")]
        private static void __Sleep(int ms)
        {
            System.Threading.Thread.Sleep(ms);
        }


        private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_model.InputKeyword):
                    OnPropertyChanged(nameof(InputKeyword));
                    break;

                case nameof(_model.ResultMessage):
                    OnPropertyChanged(nameof(ResultMessage));
                    break;

                case nameof(_model.CountMessage):
                    OnPropertyChanged(nameof(CountMessage));
                    break;
            }
        }

        private void Programs_ErrorChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(_programs.Error)) return;

            ShowMessageBox(_programs.Error);
            _programs.ClearError();
        }

        public void Loaded(Window window)
        {
            _hWnd = new WindowInteropHelper(window).Handle;
            FileSystem.SetOwnerWindowHandle(_hWnd);

            // 検索パスが設定されていなければ設定画面を開く
            if (_settings.SearchAreas.Count <= 0)
            {
                _messenger.Send(this, new ShowSettingWindowMessage() { Index = 1 });
            }

            _model.Loaded();

#if DEBUG
            // [DEV] 検索実行
            InputKeyword = "BABA";
#endif
        }

        public void Closed()
        {
            _model.Closed();
        }

        private void ShowMessageBox(string message)
        {
            _messenger.Send(this, new ShowMessageBoxMessage(message));
        }

        public void StartClipboardWatch(Window window)
        {
            _model.StartClipboardWatch(window);
        }

        public void StopClipboardWatch()
        {
            _model.StopClipboardWatch();
        }

        private void Setting_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_settings.IsDetailVisible):
                    OnPropertyChanged(nameof(IsDetailVisible));
                    OnPropertyChanged(nameof(IsTipsVisible));
                    break;

                case nameof(_settings.IsTopmost):
                    OnPropertyChanged(nameof(IsTopmost));
                    break;

                case nameof(_settings.AllowFolder):
                    OnPropertyChanged(nameof(AllowFolder));
                    _ = SearchAsync(true);
                    break;
            }
        }


        public void RestoreWindowPlacement(Window window)
        {
            WindowPlacementTools.RestoreWindowPlacement(window, _settings.WindowPlacement);
        }

        public void StoreWindowPlacement(Window window)
        {
            _settings.WindowPlacement = WindowPlacementTools.StoreWindowPlacement(window, true);
        }

        public void StoreListViewCondition(List<ListViewColumnMemento> listViewColumnMementos)
        {
            _settings.ListViewColumnMemento = listViewColumnMementos;
        }

        // キーワード即時設定
        public void SetKeyword(string keyword)
        {
            _model.SetKeyword(keyword);
        }

        // キーワード遅延設定
        public void SetKeywordDelay(string keyword)
        {
            _model.SetKeywordDelay(keyword);
        }


        public void AddHistory()
        {
            _model.AddHistory();
        }


        [RelayCommand]
        private void ShowProperty()
        {
            if (SelectedItem is null) return;

            try
            {
                ShellFileResource.OpenProperty(_hWnd, SelectedItem.Path);
            }
            catch (Exception ex)
            {
                _messenger.Send(this, new ShowMessageBoxMessage(ex.Message, MessageBoxImage.Error));
            }
        }


        [RelayCommand]
        private void Delete(System.Collections.IList? items)
        {
            if (items is null) return;
            Delete(items.Cast<FileContent>().ToList(), true);
        }

        /// <summary>
        /// ファイル削除
        /// </summary>
        /// <param name="items"></param>
        /// <param name="confirm"></param>
        private void Delete(IList<FileContent> items, bool confirm)
        {
            if (items.Count == 0) return;

            if (confirm)
            {
                var text = (items.Count == 1)
                    ? ResourceService.GetString("@Message.ConfirmDeleteFile") + "\n\n" + items[0].Path
                    : ResourceService.GetFormatString("@Message.ConfirmDeleteFiles", items.Count);
                var dialogMessage = new ShowMessageBoxMessage(text, null, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                _messenger.Send(this, dialogMessage);
                if (dialogMessage.Result != MessageBoxResult.OK) return;
            }

            try
            {
                FileSystem.SendToRecycleBin(items.Select(e => e.Path).ToList());
            }
            catch (Exception ex)
            {
                _messenger.Send(this, new ShowMessageBoxMessage(ResourceService.GetString("@Message.DeleteFailed") + "\n\n" + ex.Message, MessageBoxImage.Error));
            }
        }


        /// <summary>
        /// 名前変更
        /// </summary>
        /// <param name="file"></param>
        /// <param name="newValue"></param>
        public void Rename(FileContent file, string newValue, bool confirm)
        {
            var folder = System.IO.Path.GetDirectoryName(file.Path) ?? "";

            var src = file.Path;
            var dst = System.IO.Path.Combine(folder, newValue);

            // directory merge?
            if (file.IsDirectory && System.IO.Directory.Exists(dst))
            {
                if (confirm)
                {
                    var msg = ResourceService.GetFormatString("@Message.ConfirmFolderMerge", newValue);
                    var messageBox = new ShowMessageBoxMessage(msg, ResourceService.GetString("@Message.ConfirmFolderMerge.Title"), MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    _messenger.Send(this, messageBox);
                    if (messageBox.Result != MessageBoxResult.OK) return;
                }

                try
                {
                    FileSystem.MergeDirectory(src, dst);
                }
                catch (Exception ex)
                {
                    _messenger.Send(this, new ShowMessageBoxMessage(ResourceService.GetString("@Message.FolderMergeFailed") + "\n\n" + ex.Message, MessageBoxImage.Error));
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
                    _messenger.Send(this, new ShowMessageBoxMessage(ResourceService.GetString("@Message.RenameFailed") + "\n\n" + ex.Message, MessageBoxImage.Error));
                }
            }
        }


        [RelayCommand]
        private async Task SearchAsync()
        {
            await SearchAsync(true);
        }

        public async Task SearchAsync(bool isForce)
        {
            await _model.SearchAsync(isForce);
            AddHistory();
        }

        [RelayCommand]
        public void WebSearch()
        {
            _model.WebSearch();
        }

        [RelayCommand]
        private void ToggleDetailVisible()
        {
            _settings.IsDetailVisible = !_settings.IsDetailVisible;
        }

        [RelayCommand]
        private void ShowSetting()
        {
            _messenger.Send(this, new ShowSettingWindowMessage());
        }

        [RelayCommand]
        private void Rename()
        {
            if (SelectedItem is null) return;
            _messenger.Send(this, new RenameItemMessage(SelectedItem));
        }

        [RelayCommand]
        private void OpenPlace(System.Collections.IList? items)
        {
            if (items is null) return;
            _model.OpenPlace(items.Cast<FileContent>().ToList());
        }

        [RelayCommand]
        private void Copy(System.Collections.IList? items)
        {
            if (items is null) return;
            _model.CopyFilesToClipboard(items.Cast<FileContent>().ToList());
        }

        [RelayCommand]
        private void CopyName(System.Collections.IList? items)
        {
            if (items is null) return;
            _model.CopyNameToClipboard(items.Cast<FileContent>().ToList());
        }

        [RelayCommand]
        public void OpenDefault(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.ExecuteDefault(items.Cast<FileContent>().ToList());
        }

        [RelayCommand]
        public void OpenExternalProgram(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileContent>().ToList());
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram(OpenSelectedExternalProgramArgs args)
        {
            if (args.Items is null) return;
            _programs.Execute(args.Items.Cast<FileContent>().ToList(), args.ProgramId);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram1(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileContent>().ToList(), 1);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram2(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileContent>().ToList(), 2);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram3(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileContent>().ToList(), 3);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram4(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileContent>().ToList(), 4);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram5(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileContent>().ToList(), 5);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram6(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileContent>().ToList(), 6);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram7(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileContent>().ToList(), 7);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram8(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileContent>().ToList(), 8);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram9(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileContent>().ToList(), 9);
        }

    }



    public record class OpenSelectedExternalProgramArgs(System.Collections.IList? Items, int ProgramId);

}
