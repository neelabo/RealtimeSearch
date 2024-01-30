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
using NeeLaboratory.RealtimeSearch.Models;
using NeeLaboratory.RealtimeSearch.Clipboards;
using NeeLaboratory.RealtimeSearch.Windows;
using NeeLaboratory.Threading;
using CommunityToolkit.Mvvm.Input;
using System.CodeDom.Compiler;

namespace NeeLaboratory.RealtimeSearch
{
    public partial class MainWindowViewModel : BindableBase
    {
        private readonly MainModel _model;
        private readonly AppConfig _appConfig;
        private readonly Messenger _messenger;
        private readonly string _defaultWindowTitle;
        private bool _isRenaming;
        private readonly ExternalProgramCollection _programs;
        private FileItem? _selectedItem;


        public MainWindowViewModel(AppConfig appConfig, Messenger messenger)
        {
            _appConfig = appConfig;
            _appConfig.PropertyChanged += Setting_PropertyChanged;

            _messenger = messenger;
            
            _model = new MainModel(_appConfig);
            _model.PropertyChanged += Model_PropertyChanged;


            _programs = new ExternalProgramCollection(_appConfig);
            _programs.AddPropertyChanged(nameof(_programs.Error), Programs_ErrorChanged);

            _defaultWindowTitle = AppModel.AppInfo.ProductName;
        }


        public event EventHandler? SearchResultChanged
        {
            add { _model.SearchResultChanged += value; }
            remove { _model.SearchResultChanged -= value; }
        }


        public Search Search => _model.Search;


        public FileItem? SelectedItem
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
                    RaisePropertyChanged(nameof(InputKeyword));
                    break;

                case nameof(_model.ResultMessage):
                    RaisePropertyChanged(nameof(ResultMessage));
                    break;
            }
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

            _model.Loaded();
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
                case nameof(_appConfig.IsDetailVisible):
                    RaisePropertyChanged(nameof(IsDetailVisibled));
                    RaisePropertyChanged(nameof(IsTipsVisibled));
                    break;

                case nameof(_appConfig.IsTopmost):
                    RaisePropertyChanged(nameof(IsTopmost));
                    break;

                case nameof(_appConfig.AllowFolder):
                    RaisePropertyChanged(nameof(AllowFolder));
                    _ = SearchAsync(true);
                    break;
            }
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
                ShellFileResource.OpenProperty(Application.Current.MainWindow, SelectedItem.Path);
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
            Delete(items.Cast<FileItem>().ToList(), true);
        }

        /// <summary>
        /// ファイル削除
        /// </summary>
        /// <param name="items"></param>
        /// <param name="confirm"></param>
        private void Delete(IList<FileItem> items, bool confirm)
        {
            if (items.Count == 0) return;

            if (confirm)
            {
                var text = (items.Count == 1)
                    ? $"このファイルをごみ箱に移動しますか？\n\n{items[0].Path}"
                    : $"これらの {items.Count} 個の項目をごみ箱に移しますか？";
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
                _messenger.Send(this, new ShowMessageBoxMessage($"ファイル削除に失敗しました\n\n原因: {ex.Message}", MessageBoxImage.Error));
            }
        }

        /// <summary>
        /// 名前変更
        /// </summary>
        /// <param name="file"></param>
        /// <param name="newValue"></param>
        public void Rename(FileItem file, string newValue, bool confirm)
        {
            var folder = System.IO.Path.GetDirectoryName(file.Path) ?? "";

            var src = file.Path;
            var dst = System.IO.Path.Combine(folder, newValue);

            // directory merge?
            if (file.IsDirectory && System.IO.Directory.Exists(dst))
            {
                if (confirm)
                {
                    var msg = $"この宛先には既に '{newValue}' フォルダーが存在します。\n\n同じ名前のファイルがある場合、かっこで囲まれた番号が付加され、区別されます。\n\nフォルダーを統合しますか？";
                    var messageBox = new ShowMessageBoxMessage(msg, "フォルダーの上書き確認", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    _messenger.Send(this, messageBox);
                    if (messageBox.Result != MessageBoxResult.OK) return;
                }

                try
                {
                    FileSystem.MergeDirectory(src, dst);
                }
                catch (Exception ex)
                {
                    _messenger.Send(this, new ShowMessageBoxMessage($"フォルダーの統合に失敗しました\n\n原因: {ex.Message}", MessageBoxImage.Error));
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
            _appConfig.IsDetailVisible = !_appConfig.IsDetailVisible;
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
            _model.OpenPlace(items.Cast<FileItem>().ToList());
        }

        [RelayCommand]
        private void Copy(System.Collections.IList? items)
        {
            if (items is null) return;
            _model.CopyFilesToClipboard(items.Cast<FileItem>().ToList());
        }

        [RelayCommand]
        private void CopyName(System.Collections.IList? items)
        {
            if (items is null) return;
            _model.CopyNameToClipboard(items.Cast<FileItem>().ToList());
        }

        [RelayCommand]
        public void OpenDefault(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.ExecuteDefault(items.Cast<FileItem>().ToList());
        }

        [RelayCommand]
        public void OpenExternalProgram(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileItem>().ToList());
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram(OpenSelectedExternalProgramArgs args)
        {
            if (args.Items is null) return;
            _programs.Execute(args.Items.Cast<FileItem>().ToList(), args.ProgramId);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram1(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileItem>().ToList(), 1);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram2(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileItem>().ToList(), 2);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram3(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileItem>().ToList(), 3);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram4(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileItem>().ToList(), 4);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram5(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileItem>().ToList(), 5);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram6(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileItem>().ToList(), 6);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram7(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileItem>().ToList(), 7);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram8(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileItem>().ToList(), 8);
        }

        [RelayCommand]
        public void OpenSelectedExternalProgram9(System.Collections.IList? items)
        {
            if (items is null) return;
            _programs.Execute(items.Cast<FileItem>().ToList(), 9);
        }

    }



    public record class OpenSelectedExternalProgramArgs(System.Collections.IList? Items, int ProgramId);

}
