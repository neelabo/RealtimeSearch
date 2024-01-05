using NeeLaboratory.IO.Search.FileNode;
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
        public static readonly RoutedCommand OpenCommand = new("OpenCommand", typeof(MainWindow));
        public static readonly RoutedCommand OpenCommand1 = new("OpenCommand1", typeof(MainWindow));
        public static readonly RoutedCommand OpenCommand2 = new("OpenCommand2", typeof(MainWindow));
        public static readonly RoutedCommand OpenCommand3 = new("OpenCommand3", typeof(MainWindow));
        public static readonly RoutedCommand OpenCommandDefault = new("OpenCommandDefault", typeof(MainWindow));
        public static readonly RoutedCommand CopyCommand = new("CopyCommand", typeof(MainWindow));
        public static readonly RoutedCommand OpenPlaceCommand = new("OpenPlaceCommand", typeof(MainWindow));
        public static readonly RoutedCommand CopyNameCommand = new("CopyNameCommand", typeof(MainWindow));
        public static readonly RoutedCommand RenameCommand = new("RenameCommand", typeof(MainWindow));
        public static readonly RoutedCommand DeleteCommand = new("DeleteCommand", typeof(MainWindow));
        public static readonly RoutedCommand SearchCommand = new("SearchCommand", typeof(MainWindow));
        public static readonly RoutedCommand WebSearchCommand = new("WebSearchCommand", typeof(MainWindow));
        public static readonly RoutedCommand PropertyCommand = new("PropertyCommand", typeof(MainWindow));
        public static readonly RoutedCommand ToggleAllowFolderCommand = new("ToggleAllowFolderCommand", typeof(MainWindow));

        private readonly MainWindowViewModel _vm;

        private Point _dragStart;
        private ListViewItem? _dragDowned;

        private GridViewColumnHeader? _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;
        private bool _initialized;


        public MainWindow()
        {
            InitializeComponent();

            var messenger = new Messenger();
            messenger.Register<ShowMessageBoxMessage>(ShowMessageBox);
            messenger.Register<ShowSettingWindowMessage>(ShowSettingWindow);

            _vm = new MainWindowViewModel(App.AppConfig, messenger);
            this.DataContext = _vm;

            RegistRoutedCommand();

            _vm.SearchResultChanged += ViewModel_FilesChanged;

            this.SourceInitialized += MainWindow_SourceInitialized;
            this.KeyDown += MainWindow_KeyDown;
            this.MouseLeftButtonDown += (s, e) => this.RenameManager.Stop();
            this.MouseRightButtonDown += (s, e) => this.RenameManager.Stop();
            this.Deactivated += (s, e) => this.RenameManager.Stop();

            RestoreListViewMemento(App.AppConfig.ListViewColumnMemento);
        }


        #region Messaging

        private void ShowMessageBox(object? sender, ShowMessageBoxMessage e)
        {
            e.Result = MessageBox.Show(e.Message, e.Caption, e.Button, e.Icon);
        }

        private void ShowSettingWindow(object? sender, ShowSettingWindowMessage e)
        {
            ShowSettingWindow();
        }

        #endregion Messaging

        #region Methods

        private void ViewModel_FilesChanged(object? sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(GridViewColumnHeader_Reset), null);
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            _vm.RestoreWindowPlacement(this);
        }

        private void Window_Loaded(object? sender, RoutedEventArgs e)
        {
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (_initialized) return;
            _initialized = true;

            _vm.Loaded();
            _vm.StartClipboardWatch(this);
        }

        private void Window_Closing(object? sender, EventArgs e)
        {
            _vm.StoreListViewCondition(CreateListViewMemento());
            _vm.StoreWindowPlacement(this);
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            _vm.StopClipboardWatch();
        }

        private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.keyword.Focus();
                e.Handled = true;
            }
        }

        private void ListViewItem_MouseDoubleClick(object? sender, MouseButtonEventArgs e)
        {
            if (((ListViewItem?)sender)?.Content is not FileItem file) return;

            _vm.Execute(new List<FileItem>() { file });
        }

        private async void Keyword_KeyDown(object? sender, KeyEventArgs e)
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

        private void Keyword_LostFocus(object? sender, RoutedEventArgs e)
        {
            _vm.AddHistory();
        }

        private void Keyword_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (e.OriginalSource is TextBox textBox)
            {
                _vm.SetKeywordDelay(textBox.Text);
            }
        }

        private void SettingButton_Click(object? sender, RoutedEventArgs e)
        {
            ShowSettingWindow();
        }

        private void ShowSettingWindow()
        {
            var window = new SettingWindow(App.AppConfig)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            window.ShowDialog();
        }

        #endregion Methods

        #region Rename

        // NOTE: 名前変更によって選択解除されてしまうことがあるので編集前に選択番号を記憶しておく
        private int _renameIndex;

        private void PopupRenameTextBox(FileItem item)
        {
            if (item == null) return;

            var listViewItem = VisualTreeTools.GetListViewItemFromItem(this.ResultListView, item);
            var textBlock = VisualTreeTools.FindVisualChild<TextBlock>(listViewItem, "FileNameTextBlock");

            if (textBlock != null)
            {
                var rename = new RenameControl(textBlock)
                {
                    IsCoerceFileName = true,
                    IsSelectedWithoutExtension = System.IO.File.Exists(item.Path)
                };
                rename.Closing += Rename_Closing;
                rename.Closed += Rename_Closed;
                _renameIndex = this.ResultListView.SelectedIndex;

                this.RenameManager.Open(rename);
                _vm.IsRenaming = true;
            }
        }

        private void Rename_Closing(object? sender, RenameClosingEventArgs e)
        {
            //Debug.WriteLine($"{ev.OldValue} => {ev.NewValue}");
            if (e.OldValue != e.NewValue)
            {
                if (this.ResultListView.SelectedItem is FileItem file)
                {
                    _vm.Rename(file, e.NewValue);
                }
            }
        }

        private void Rename_Closed(object? sender, RenameClosedEventArgs e)
        {
            _vm.IsRenaming = false;

            var listViewItem = VisualTreeTools.FindAncestor<ListViewItem>(e.Target);
            listViewItem?.Focus();

            if (e.Navigate != 0)
            {
                RenameNext(e.Navigate);
            }
            else
            {
                this.ResultListView.SelectedIndex = _renameIndex;
            }
        }

        private void RenameNext(int delta)
        {
            if (_renameIndex < 0) return;

            // 選択項目を1つ移動
            this.ResultListView.SelectedIndex = (_renameIndex + this.ResultListView.Items.Count + delta) % this.ResultListView.Items.Count;
            this.ResultListView.UpdateLayout();

            // リネーム発動
            Rename_Executed(this.ResultListView, null);
        }

        #endregion Rename

        #region DragDrop

        // ファイルのドラッグ判定開始
        private void PreviewMouseDown_Event(object? sender, MouseButtonEventArgs e)
        {
            _dragDowned = sender as ListViewItem;
            _dragStart = e.GetPosition(_dragDowned);
        }

        // ファイルのドラッグ開始
        private void PreviewMouseMove_Event(object? sender, MouseEventArgs e)
        {
            var s = sender as ListViewItem;

            if (_dragDowned != null && _dragDowned == s && e.LeftButton == MouseButtonState.Pressed)
            {
                var current = e.GetPosition(s);
                if (Math.Abs(current.X - _dragStart.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(current.Y - _dragStart.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _dragDowned = null;

                    if (this.ResultListView.SelectedItems.Count > 0)
                    {
                        var paths = new List<string>();
                        for (int i = 0; i < this.ResultListView.SelectedItems.Count; ++i)
                        {
                            var path = (this.ResultListView.SelectedItems[i] as FileItem)?.Path;
                            if (path != null)
                            {
                                paths.Add(path);
                            }
                        }

                        var data = new DataObject();
                        data.SetData(DataFormats.FileDrop, paths.ToArray());
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
            await _vm.ToggleAllowFolderAsync();
        }

        private void Property_Executed(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as ListView)?.SelectedItem is FileItem file)
            {
                _vm.OpenProperty(file);
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
            if (items != null && items.Count > 0)
            {
                _vm.Delete(items.Cast<FileItem>().ToList());
            }
        }

        // 名前変更
        private void Rename_Executed(object? target, ExecutedRoutedEventArgs? e)
        {
            var listView = target as ListView;

            if (listView?.SelectedItem is FileItem item)
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
        private void Open_Executed(object target, ExecutedRoutedEventArgs _, ExecuteType executeType, int programId = 0)
        {
            // TODO: vm
            var items = (target as ListView)?.SelectedItems;

            if (items == null) return;

            var nodes = items.OfType<FileItem>();
            if (!nodes.Any()) return;

            switch (executeType)
            {
                default:
                case ExecuteType.Default:
                    _vm.ExecuteDefault(nodes);
                    break;
                case ExecuteType.ExternalPrograms:
                    _vm.Execute(nodes);
                    break;
                case ExecuteType.SelectedExternalProgram:
                    _vm.Execute(nodes, programId);
                    break;
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
            // TODO: vm
            var items = (target as ListView)?.SelectedItems;

            if (items == null) return;

            foreach (var item in items)
            {
                if (item is FileItem file && (System.IO.File.Exists(file.Path) || System.IO.Directory.Exists(file.Path)))
                {
                    var startInfo = new ProcessStartInfo("explorer.exe", "/select,\"" + file.Path + "\"") { UseShellExecute = false };
                    Process.Start(startInfo);
                }
            }
        }

        // ファイルのコピー
        private void Copy_Executed(object target, ExecutedRoutedEventArgs e)
        {
            var items = (target as ListView)?.SelectedItems;
            if (items is null) return;

            _vm.CopyFilesToClipboard(items.Cast<FileItem>().ToList());
        }

        // ファイル名のコピー
        private void CopyName_Executed(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as ListView)?.SelectedItem is FileItem file)
            {
                _vm.CopyNameToClipboard(file);
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

            ListSortDirection direction;

            if (e.OriginalSource is GridViewColumnHeader headerClicked)
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


                    string? header = headerClicked.Column.DisplayMemberBinding is Binding binding
                        ? binding.Path.Path
                        : headerClicked.Tag as string ?? "";

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
            if (CollectionViewSource.GetDefaultView(this.ResultListView.ItemsSource) is not ListCollectionView dataView) return;

            dataView.SortDescriptions.Clear();
            var sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        #endregion

        #region ListViewColumnMemento

        // カラムヘッダ文字列取得
        private static string? GetColumnHeaderText(GridViewColumn column)
        {
            return (column.Header as string) ?? (column.Header as GridViewColumnHeader)?.Content as string;
        }

        // リストビューカラム状態保存
        private List<ListViewColumnMemento> CreateListViewMemento()
        {
            var columns = (this.ResultListView.View as GridView)?.Columns;
            if (columns == null) return new List<ListViewColumnMemento>();

            var memento = new List<ListViewColumnMemento>();
            foreach (var column in columns)
            {
                var key = GetColumnHeaderText(column);
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
            if (!memento.Any()) return;

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
