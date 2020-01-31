﻿using NeeLaboratory.IO.Search;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NeeLaboratory.RealtimeSearch
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fieds

        public static readonly RoutedCommand OpenCommand = new RoutedCommand("OpenCommand", typeof(MainWindow));
        public static readonly RoutedCommand OpenCommand1 = new RoutedCommand("OpenCommand1", typeof(MainWindow));
        public static readonly RoutedCommand OpenCommand2 = new RoutedCommand("OpenCommand2", typeof(MainWindow));
        public static readonly RoutedCommand OpenCommand3 = new RoutedCommand("OpenCommand3", typeof(MainWindow));
        public static readonly RoutedCommand OpenCommandDefault = new RoutedCommand("OpenCommandDefault", typeof(MainWindow));
        public static readonly RoutedCommand CopyCommand = new RoutedCommand("CopyCommand", typeof(MainWindow));
        public static readonly RoutedCommand OpenPlaceCommand = new RoutedCommand("OpenPlaceCommand", typeof(MainWindow));
        public static readonly RoutedCommand CopyNameCommand = new RoutedCommand("CopyNameCommand", typeof(MainWindow));
        public static readonly RoutedCommand RenameCommand = new RoutedCommand("RenameCommand", typeof(MainWindow));
        public static readonly RoutedCommand DeleteCommand = new RoutedCommand("DeleteCommand", typeof(MainWindow));
        public static readonly RoutedCommand SearchCommand = new RoutedCommand("SearchCommand", typeof(MainWindow));
        public static readonly RoutedCommand WebSearchCommand = new RoutedCommand("WebSearchCommand", typeof(MainWindow));
        public static readonly RoutedCommand PropertyCommand = new RoutedCommand("PropertyCommand", typeof(MainWindow));
        public static readonly RoutedCommand ToggleAllowFolderCommand = new RoutedCommand("ToggleAllowFolderCommand", typeof(MainWindow));

        private MainWindowViewModel _vm;

        private Point _dragStart;
        private ListViewItem _dragDowned;

        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

        #endregion Fields

        #region Constructors

        public MainWindow()
        {
            InitializeComponent();

            _vm = new MainWindowViewModel();
            this.DataContext = _vm;

            RegistRoutedCommand();

            _vm.SearchResultChanged += ViewModel_FilesChanged;

            this.MouseLeftButtonDown += (s, e) => this.RenameManager.Stop();
            this.MouseRightButtonDown += (s, e) => this.RenameManager.Stop();
            this.Deactivated += (s, e) => this.RenameManager.Stop();

            this.ResultListView.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(ResultListView_ScrollChanged));

            _vm.LoadSetting();
            _vm.RestoreWindowPlacement(this);
            RestoreListViewMemento(_vm.Setting.ListViewColumnMemento);

            FileSystem.InitializeDefaultResource();
        }

        #endregion Constructors

        #region Methods

        private void ViewModel_FilesChanged(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(GridViewColumnHeader_Reset), null);
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _vm.Open(this);

            // 検索パスが設定されていなければ設定画面を開く
            if (_vm.Setting.SearchAreas.Count <= 0)
            {
                ShowSettingWindow();
            }
        }

        private void Window_Closing(object sender, EventArgs e)
        {
            _vm.Setting.ListViewColumnMemento = CreateListViewMemento();
            _vm.StoreWindowPlacement(this);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _vm.Close();
        }

        void ResultListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            this.RenameManager.Stop();
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            NodeContent file = ((ListViewItem)sender).Content as NodeContent;
            if (file == null) return;

            Execute(file);
        }

        private async void Keyword_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                if (e.Key == Key.Enter)
                {
                    _vm.SetKeyword(comboBox.Text);
                    await _vm.SearchAsync(false);
                    _vm.AddHistory();
                }
            }
        }

        private void Keyword_LostFocus(object sender, RoutedEventArgs e)
        {
            _vm.AddHistory();
        }

        private void Keyword_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = e.OriginalSource as TextBox;
            if (textBox != null)
            {
                _vm.SetKeywordDelay(textBox.Text);
            }
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingWindow();
        }

        private void ShowSettingWindow()
        {
            var window = new SettingWindow(_vm.Setting);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        #endregion Methods

        #region Rename

        private void PopupRenameTextBox(NodeContent item)
        {
            if (item == null) return;

            var listViewItem = VisualTreeTools.GetListViewItemFromItem(this.ResultListView, item);
            var textBlock = VisualTreeTools.FindVisualChild<TextBlock>(listViewItem, "FileNameTextBlock");

            if (textBlock != null)
            {
                var rename = new RenameControl();
                rename.IsSelectedWithoutExtension = System.IO.File.Exists(item.Path);
                rename.Target = textBlock;
                rename.Closing += (s, ev) =>
                {
                    //Debug.WriteLine($"{ev.OldValue} => {ev.NewValue}");
                    if (ev.OldValue != ev.NewValue)
                    {
                        NodeContent file = this.ResultListView.SelectedItem as NodeContent;
                        var src = file.Path;
                        var dst = Rename(file, ev.NewValue);
                        if (dst != null)
                        {
                            _vm.Rename(src, dst);
                        }
                    }
                };
                rename.Closed += (s, ev) =>
                {
                    listViewItem.Focus();
                    if (ev.MoveRename != 0)
                    {
                        RenameNext(ev.MoveRename);
                    }
                };
                rename.Close += (s, ev) =>
                {
                    _vm.IsRenaming = false;
                };

                this.RenameManager.Open(rename);
                _vm.IsRenaming = true;
            }
        }

        private void RenameNext(int delta)
        {
            if (this.ResultListView.SelectedIndex < 0) return;

            // 選択項目を1つ移動
            this.ResultListView.SelectedIndex = (this.ResultListView.SelectedIndex + this.ResultListView.Items.Count + delta) % this.ResultListView.Items.Count;
            this.ResultListView.UpdateLayout();

            // リネーム発動
            Rename_Executed(this.ResultListView, null);
        }

        /// <summary>
        /// 名前変更
        /// </summary>
        /// <param name="file"></param>
        /// <param name="newName"></param>
        /// <returns>成功した場合は新しいフルパス。失敗した場合はnull</returns>
        private string Rename(NodeContent file, string newName)
        {
            if (file == null || string.IsNullOrWhiteSpace(newName)) return null;

            //ファイル名に使用できない文字
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            int invalidCharsIndex = newName.IndexOfAny(invalidChars);
            if (invalidCharsIndex >= 0)
            {
                // 確認
                MessageBox.Show($"ファイル名に使用できない文字が含まれています。( {newName[invalidCharsIndex]} )", "名前の変更の確認", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            string src = file.Path;
            string dst = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(src), newName);
            if (src == dst) return null;

            bool isFile = System.IO.File.Exists(src);

            // 拡張子変更警告
            if (isFile && System.IO.Path.GetExtension(src).ToLower() != System.IO.Path.GetExtension(dst).ToLower())
            {
                // 確認
                var resut = MessageBox.Show($"拡張子を変更すると、ファイルが使えなくなる可能性があります。\n\n変更しますか？", "名前の変更の確認", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (resut != MessageBoxResult.OK)
                {
                    return null;
                }
            }

            // 重複ファイル名回避
            if (string.Compare(src, dst, true) == 0)
            {
                // nop.
            }
            else if (System.IO.File.Exists(dst) || System.IO.Directory.Exists(dst))
            {
                string dstBase = dst;
                string dir = System.IO.Path.GetDirectoryName(dst);
                string name = System.IO.Path.GetFileNameWithoutExtension(dst);
                string ext = System.IO.Path.GetExtension(dst);
                int count = 1;

                do
                {
                    dst = $"{dir}\\{name} ({++count}){ext}";
                }
                while (System.IO.File.Exists(dst) || System.IO.Directory.Exists(dst));

                // 確認
                var resut = MessageBox.Show($"{System.IO.Path.GetFileName(dstBase)} は既に存在します。\n{System.IO.Path.GetFileName(dst)} に名前を変更しますか？", "名前の変更の確認", MessageBoxButton.OKCancel);
                if (resut != MessageBoxResult.OK)
                {
                    return null;
                }
            }

            // 名前変更実行
            try
            {
                if (System.IO.Directory.Exists(src))
                {
                    System.IO.Directory.Move(src, dst);
                }
                else
                {
                    System.IO.File.Move(src, dst);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("名前の変更に失敗しました。\n\n" + ex.Message, "通知", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            return dst;
        }

        #endregion Rename

        #region Execute file

        private void Execute(NodeContent file)
        {
            try
            {
                foreach (var program in _vm.Setting.ExternalPrograms)
                {
                    if (program.CheckExtensions(file.Path))
                    {
                        Execute(file, program);
                        return;
                    }
                }
                var startInfo = new System.Diagnostics.ProcessStartInfo(file.Path) { UseShellExecute = true };
                System.Diagnostics.Process.Start(startInfo); // terminator
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                MessageBox.Show(e.Message, "実行失敗", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Execute(NodeContent file, int programId)
        {
            try
            {
                Execute(file, _vm.Setting.ExternalPrograms[programId - 1]);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                MessageBox.Show(e.Message, "実行失敗", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Execute(NodeContent file, ExternalProgram program)
        {
            if (program.ProgramType == ExternalProgramType.Normal)
            {
                if (!string.IsNullOrWhiteSpace(program.Program))
                {
                    var commandName = program.Program;
                    var arguments = ReplaceKeyword(program.Parameter, file);
                    var startInfo = new System.Diagnostics.ProcessStartInfo(commandName, arguments) { UseShellExecute = false };
                    System.Diagnostics.Process.Start(startInfo);
                    return;
                }
                else
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo(file.Path) { UseShellExecute = true };
                    System.Diagnostics.Process.Start(startInfo);
                    return;
                }
            }

            if (program.ProgramType == ExternalProgramType.Uri)
            {
                if (!string.IsNullOrWhiteSpace(program.Protocol))
                {
                    var protocol = ReplaceKeyword(program.Protocol, file);
                    var startInfo = new System.Diagnostics.ProcessStartInfo(protocol) { UseShellExecute = true };
                    System.Diagnostics.Process.Start(startInfo);
                    return;
                }
            }
        }

        private string ReplaceKeyword(string s, NodeContent file)
        {
            var uriData = Uri.EscapeDataString(file.Path);

            s = s.Replace(ExternalProgram.KeyUri, uriData);
            s = s.Replace(ExternalProgram.KeyFile, file.Path);

            return s;
        }

        private void ExecuteDefault(NodeContent file)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo(file.Path) { UseShellExecute = true };
                System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                MessageBox.Show(e.Message, "実行失敗", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion Execute file

        #region DragDrop

        // ファイルのドラッグ判定開始
        private void PreviewMouseDown_Event(object sender, MouseButtonEventArgs e)
        {
            _dragDowned = sender as ListViewItem;
            _dragStart = e.GetPosition(_dragDowned);
        }

        // ファイルのドラッグ開始
        private void PreviewMouseMove_Event(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var s = sender as ListViewItem;
            var pn = s.Content as NodeContent;

            if (_dragDowned != null && _dragDowned == s && e.LeftButton == MouseButtonState.Pressed)
            {
                var current = e.GetPosition(s);
                if (Math.Abs(current.X - _dragStart.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(current.Y - _dragStart.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _dragDowned = null;

                    if (this.ResultListView.SelectedItems.Count > 0)
                    {
                        string[] paths = new string[this.ResultListView.SelectedItems.Count];
                        for (int i = 0; i < this.ResultListView.SelectedItems.Count; ++i)
                        {
                            paths[i] = ((NodeContent)this.ResultListView.SelectedItems[i]).Path;
                        }

                        DataObject data = new DataObject();
                        data.SetData(DataFormats.FileDrop, paths);
                        DragDrop.DoDragDrop(s, data, DragDropEffects.Copy);
                    }
                }
            }
        }

        #endregion DragDrop

        #region Routed Commands

        private void RegistRoutedCommand()
        {
            OpenCommand.InputGestures.Add(new KeyGesture(Key.Enter));
            this.ResultListView.CommandBindings.Add(new CommandBinding(OpenCommand, Open_Executed));

            OpenCommand1.InputGestures.Add(new KeyGesture(Key.D1, ModifierKeys.Control));
            this.ResultListView.CommandBindings.Add(new CommandBinding(OpenCommand1, OpenEx1_Executed));

            OpenCommand2.InputGestures.Add(new KeyGesture(Key.D2, ModifierKeys.Control));
            this.ResultListView.CommandBindings.Add(new CommandBinding(OpenCommand2, OpenEx2_Executed));

            OpenCommand3.InputGestures.Add(new KeyGesture(Key.D3, ModifierKeys.Control));
            this.ResultListView.CommandBindings.Add(new CommandBinding(OpenCommand3, OpenEx3_Executed));

            OpenCommandDefault.InputGestures.Add(new KeyGesture(Key.Enter, ModifierKeys.Control));
            this.ResultListView.CommandBindings.Add(new CommandBinding(OpenCommandDefault, OpenDefault_Executed));

            CopyCommand.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control));
            this.ResultListView.CommandBindings.Add(new CommandBinding(CopyCommand, Copy_Executed));

            this.ResultListView.CommandBindings.Add(new CommandBinding(OpenPlaceCommand, OpenPlace_Executed));

            CopyNameCommand.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift));
            this.ResultListView.CommandBindings.Add(new CommandBinding(CopyNameCommand, CopyName_Executed));

            RenameCommand.InputGestures.Add(new KeyGesture(Key.F2));
            this.ResultListView.CommandBindings.Add(new CommandBinding(RenameCommand, Rename_Executed));

            DeleteCommand.InputGestures.Add(new KeyGesture(Key.Delete));
            this.ResultListView.CommandBindings.Add(new CommandBinding(DeleteCommand, Delete_Executed));

            SearchCommand.InputGestures.Add(new KeyGesture(Key.F5));
            SearchCommand.InputGestures.Add(new KeyGesture(Key.R, ModifierKeys.Control));
            this.CommandBindings.Add(new CommandBinding(SearchCommand, Search_Executed));

            WebSearchCommand.InputGestures.Add(new KeyGesture(Key.F, ModifierKeys.Control));
            this.CommandBindings.Add(new CommandBinding(WebSearchCommand, WebSearch_Executed));

            this.ResultListView.CommandBindings.Add(new CommandBinding(PropertyCommand, Property_Executed));

            this.CommandBindings.Add(new CommandBinding(ToggleAllowFolderCommand, ToggleAllowFolder_Executed));
        }

        private async void ToggleAllowFolder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _vm.Setting.ToggleAllowFolder();
            await _vm.SearchAsync(true);
        }

        private void Property_Executed(object target, ExecutedRoutedEventArgs e)
        {
            NodeContent file = (target as ListView)?.SelectedItem as NodeContent;
            if (file != null)
            {
                try
                {
                    FileSystem.OpenProperty(this, file.Path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void Search_Executed(object target, ExecutedRoutedEventArgs e)
        {
            await _vm.SearchAsync(true);
            _vm.AddHistory();
        }

        private void WebSearch_Executed(object target, ExecutedRoutedEventArgs e)
        {
            _vm.WebSearch();
        }

        // ファイル削除
        private void Delete_Executed(object target, ExecutedRoutedEventArgs e)
        {
            var items = (target as ListView)?.SelectedItems;
            if (items != null && items.Count >= 1)
            {
                var message = (items.Count == 1)
                    ? $"このファイルをごみ箱に移動しますか？\n\n{((NodeContent)items[0]).Path}"
                    : $"これらの {items.Count} 個の項目をごみ箱に移しますか？";

                var result = MessageBox.Show(message, "削除確認", MessageBoxButton.OKCancel, MessageBoxImage.Question);


                if (result == MessageBoxResult.OK)
                {
                    try
                    {
                        // ゴミ箱に捨てる
                        foreach (var item in items)
                        {
                            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(((NodeContent)item).Path, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"ファイル削除に失敗しました\n\n原因: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // 名前変更
        private void Rename_Executed(object target, ExecutedRoutedEventArgs e)
        {
            var listView = target as ListView;
            var item = listView?.SelectedItem as NodeContent;

            if (item != null)
            {
                PopupRenameTextBox(item);
            }
        }

        // 外部アプリ実行タイプ
        private enum ExecuteType
        {
            Default,
            ExternalPrograms,
            SelectedExternalProgram,
        }

        // ファイルを開く
        private void Open_Executed(object target, ExecutedRoutedEventArgs e, ExecuteType executeType, int programId = 0)
        {
            var items = (target as ListView)?.SelectedItems;

            if (items == null) return;

            foreach (var item in items)
            {
                NodeContent file = item as NodeContent;
                if (file != null && (System.IO.File.Exists((string)file.Path) || System.IO.Directory.Exists((string)file.Path)))
                {
                    switch (executeType)
                    {
                        default:
                        case ExecuteType.Default:
                            ExecuteDefault(file);
                            break;
                        case ExecuteType.ExternalPrograms:
                            Execute(file);
                            break;
                        case ExecuteType.SelectedExternalProgram:
                            Execute(file, programId);
                            break;
                    }
                }
            }
        }

        private void OpenEx1_Executed(object target, ExecutedRoutedEventArgs e)
        {
            Open_Executed(target, e, ExecuteType.SelectedExternalProgram, 1);
        }

        private void OpenEx2_Executed(object target, ExecutedRoutedEventArgs e)
        {
            Open_Executed(target, e, ExecuteType.SelectedExternalProgram, 2);
        }

        private void OpenEx3_Executed(object target, ExecutedRoutedEventArgs e)
        {
            Open_Executed(target, e, ExecuteType.SelectedExternalProgram, 3);
        }

        private void Open_Executed(object target, ExecutedRoutedEventArgs e)
        {
            Open_Executed(target, e, ExecuteType.ExternalPrograms);
        }

        // ファイルを開く(既定)
        private void OpenDefault_Executed(object target, ExecutedRoutedEventArgs e)
        {
            Open_Executed(target, e, ExecuteType.Default);
        }

        // ファイルの場所を開く
        private void OpenPlace_Executed(object target, ExecutedRoutedEventArgs e)
        {
            var items = (target as ListView)?.SelectedItems;

            if (items == null) return;

            foreach (var item in items)
            {
                NodeContent file = item as NodeContent;
                if (file != null && (System.IO.File.Exists((string)file.Path) || System.IO.Directory.Exists((string)file.Path)))
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo("explorer.exe", (string)("/select,\"" + file.Path + "\"")) { UseShellExecute = false };
                    System.Diagnostics.Process.Start(startInfo);
                }
            }
        }

        // ファイルのコピー
        private void Copy_Executed(object target, ExecutedRoutedEventArgs e)
        {
            var files = new System.Collections.Specialized.StringCollection();

            foreach (var item in (target as ListView)?.SelectedItems)
            {
                NodeContent file = item as NodeContent;
                if (file != null) files.Add(file.Path);
            }

            if (files.Count > 0) Clipboard.SetFileDropList(files);
        }

        // ファイル名のコピー
        private void CopyName_Executed(object target, ExecutedRoutedEventArgs e)
        {
            NodeContent file = (target as ListView)?.SelectedItem as NodeContent;
            if (file != null)
            {
                string text = System.IO.Path.GetFileNameWithoutExtension(file.Path);
                _vm.SetClipboard(text);
            }
        }

        #endregion Routed Commands

        #region Sort ListView

        private void GridViewColumnHeader_Reset()
        {
            if (_lastHeaderClicked != null)
            {
                _lastHeaderClicked.Column.HeaderTemplate = null;
                _lastHeaderClicked = null;
            }
            _lastDirection = ListSortDirection.Ascending;
        }

        private void GridViewColumnHeader_ClickHandler(object sender, RoutedEventArgs e)
        {
            if (this.ResultListView.ItemsSource == null) return;

            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    Binding binding = headerClicked.Column.DisplayMemberBinding as Binding;

                    string header = binding != null
                        ? (headerClicked.Column.DisplayMemberBinding as Binding).Path.Path
                        : headerClicked.Tag as string;

                    GridViewColumnHeader_Sort(header, direction);

                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastHeaderClicked.Column.HeaderTemplate = null;
                    }

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

        private void GridViewColumnHeader_Sort(string sortBy, ListSortDirection direction)
        {
            ListCollectionView dataView = CollectionViewSource.GetDefaultView(this.ResultListView.ItemsSource) as ListCollectionView;

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        #endregion

        #region ListViewColumnMemento

        // カラムヘッダ文字列取得
        private string GetColumnHeaderText(GridViewColumn column)
        {
            return (column.Header as string) ?? (column.Header as GridViewColumnHeader)?.Content as string;
        }

        // リストビューカラム状態保存
        private List<ListViewColumnMemento> CreateListViewMemento()
        {
            var columns = (this.ResultListView.View as GridView)?.Columns;
            if (columns == null) return null;

            var memento = new List<ListViewColumnMemento>();
            foreach (var column in columns)
            {
                string key = GetColumnHeaderText(column);
                if (key != null)
                {
                    memento.Add(new ListViewColumnMemento() { Header = key, Width = column.Width });
                }
            }
            return memento;
        }

        // リストビューカラム状態復帰
        private void RestoreListViewMemento(List<ListViewColumnMemento> memento)
        {
            if (memento == null) return;

            var columns = (this.ResultListView.View as GridView)?.Columns;
            if (columns == null) return;

            for (int index = 0; index < memento.Count; ++index)
            {
                var item = memento[index];

                var column = columns.FirstOrDefault(e => GetColumnHeaderText(e) == item.Header);
                if (column != null)
                {
                    int oldIndex = columns.IndexOf(column);
                    if (oldIndex >= 0 && oldIndex != index)
                    {
                        columns.Move(oldIndex, index);
                    }
                    column.Width = item.Width;
                }
            }
        }

        #endregion
    }
}