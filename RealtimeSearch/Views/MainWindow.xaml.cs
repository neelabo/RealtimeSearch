using NeeLaboratory.IO.Search;
using NeeLaboratory.IO.Search.Files;
using NeeLaboratory.RealtimeSearch.Models;
using NeeLaboratory.RealtimeSearch.TextResource;
using NeeLaboratory.RealtimeSearch.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Xml.Linq;

namespace NeeLaboratory.RealtimeSearch.Views
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
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
            messenger.Register<RenameItemMessage>(RenameItem);

            _vm = new MainWindowViewModel(AppModel.Settings, messenger);
            this.DataContext = _vm;

            _vm.SearchResultChanged += ViewModel_FilesChanged;

            this.SourceInitialized += MainWindow_SourceInitialized;
            this.KeyDown += MainWindow_KeyDown;
            this.MouseLeftButtonDown += (s, e) => this.RenameManager.Stop();
            this.MouseRightButtonDown += (s, e) => this.RenameManager.Stop();
            this.Deactivated += (s, e) => this.RenameManager.Stop();

            RestoreListViewMemento(AppModel.Settings.ListViewColumnMemento);

            this.WebSearchButton.ToolTip = ResourceService.GetString("@Button.WebSearch") + " (" + new KeyGesture(Key.F, ModifierKeys.Control).GetDisplayStringForCulture(CultureInfo.CurrentCulture) + ")";
            this.RefreshButton.ToolTip = ResourceService.GetString("@Button.Refresh") + " (" + new KeyGesture(Key.F5).GetDisplayStringForCulture(CultureInfo.CurrentCulture) + ")";

#if false
            // 実験
            {
                var x = "𠀁";
                var regex1 = new Regex(@"[\u0020-\u024F-[\P{L}]]");
                var regex2 = new Regex(@"[\u0250-\uFFFF\P{L}]");
                for (int i = 0x0020; i<0x1000; i++)
                {
                    var isMatch1 = regex1.IsMatch(((char)i).ToString());
                    var mark1 = isMatch1 ? "o" : "x";
                    var isMatch2 = regex2.IsMatch(((char)i).ToString());
                    var mark2 = isMatch2 ? "o" : "x";
                    Debug.WriteLine($"{i:X4}: {mark1}{mark2}: {(char)i}");
                    Debug.Assert(isMatch1 != isMatch2);
                }
            }
#endif
        }


        #region Messaging

        private void ShowMessageBox(object? sender, ShowMessageBoxMessage e)
        {
            e.Result = MessageBox.Show(App.Current.MainWindow, e.Message, e.Caption, e.Button, e.Icon);
        }

        private void ShowSettingWindow(object? sender, ShowSettingWindowMessage e)
        {
            ShowSettingWindow(e.Index);
        }

        private void RenameItem(object? sender, RenameItemMessage e)
        {
            PopupRenameTextBox(e.Item);
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

            _vm.Loaded(this);
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
            _vm.Closed();
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
            if (((ListViewItem?)sender)?.Content is not FileContent file) return;
            _vm.OpenExternalProgram(new List<FileContent>() { file });
        }

        private async void Keyword_KeyDown(object? sender, KeyEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                if (e.Key == Key.Enter)
                {
                    _vm.SetKeyword(comboBox.Text);
                    await _vm.SearchAsync(false);
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


        private void ShowSettingWindow(int index)
        {
            var window = new SettingWindow(AppModel.Settings, index)
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

        private void PopupRenameTextBox(FileContent item)
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
                if (this.ResultListView.SelectedItem is FileContent file)
                {
                    _vm.Rename(file, e.NewValue, true);
                }
            }
        }

        private void Rename_Closed(object? sender, RenameClosedEventArgs e)
        {
            _vm.IsRenaming = false;

            if (e.Navigate != 0)
            {
                RenameNext(e.Navigate);
            }
            else
            {
                var listViewItem = (ListViewItem)(this.ResultListView.ItemContainerGenerator.ContainerFromItem(this.ResultListView.SelectedItem));
                listViewItem?.Focus();
            }
        }

        private void RenameNext(int delta)
        {
            if (_renameIndex < 0) return;

            // 選択項目を1つ移動
            this.ResultListView.SelectedIndex = (_renameIndex + this.ResultListView.Items.Count + delta) % this.ResultListView.Items.Count;
            this.ResultListView.UpdateLayout();

            // リネーム発動
            if (this.ResultListView.SelectedItem is FileContent item)
            {
                PopupRenameTextBox(item);
            }
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
                            var path = (this.ResultListView.SelectedItems[i] as FileContent)?.Path;
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
            return (column.Header as GridViewColumnHeader)?.Tag as string;
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

        #endregion ListViewColumnMemento

        #region ContextMenu

        private void ContextMenu_Opening(object sender, ContextMenuEventArgs e)
        {
            var frameworkElement = sender as FrameworkElement;
            var contextMenu = frameworkElement?.ContextMenu;
            if (contextMenu is null) return;

            var selectedItems = this.ResultListView.SelectedItems;

            contextMenu.Items.Clear();
            contextMenu.Items.Add(CreateMenuItem(ResourceService.GetString("@Menu.Open"), _vm.OpenExternalProgramCommand, selectedItems, new KeyGesture(Key.Enter)));
            contextMenu.Items.Add(CreateMenuItem(ResourceService.GetString("@Menu.OpenDefault"), _vm.OpenDefaultCommand, selectedItems, new KeyGesture(Key.Enter, ModifierKeys.Control)));

            if (_vm.Programs.Count > 0)
            {
                contextMenu.Items.Add(new Separator());
                for (int i = 0; i < _vm.Programs.Count; i++)
                {
                    var program = _vm.Programs[i];
                    var header = MenuItemTools.IntegerToAccessKey(i + 1) + " " + MenuItemTools.EscapeMenuItemString(program.Name);
                    if (i < 9)
                    {
                        contextMenu.Items.Add(CreateMenuItem(header, _vm.OpenSelectedExternalProgramCommand, new OpenSelectedExternalProgramArgs(selectedItems, i + 1), new KeyGesture(Key.D1 + i, ModifierKeys.Control)));
                    }
                    else
                    {
                        contextMenu.Items.Add(CreateMenuItem(header, _vm.OpenSelectedExternalProgramCommand, new OpenSelectedExternalProgramArgs(selectedItems, i + 1)));
                    }
                }
            }

            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(CreateMenuItem(ResourceService.GetString("@Menu.OpenFileLocation"), _vm.OpenPlaceCommand, selectedItems));
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(CreateMenuItem(ResourceService.GetString("@Menu.Copy"), _vm.CopyCommand, selectedItems, new KeyGesture(Key.C, ModifierKeys.Control)));
            contextMenu.Items.Add(CreateMenuItem(ResourceService.GetString("@Menu.CopyName"), _vm.CopyNameCommand, selectedItems, new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift)));
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(CreateMenuItem(ResourceService.GetString("@Menu.Delete"), _vm.DeleteCommand, selectedItems, new KeyGesture(Key.Delete)));
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(CreateMenuItem(ResourceService.GetString("@Menu.Rename"), _vm.RenameCommand, null, new KeyGesture(Key.F2)));
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(CreateMenuItem(ResourceService.GetString("@Menu.Property"), _vm.ShowPropertyCommand));
        }

        private MenuItem CreateMenuItem(string header, ICommand command, object? commandParameter = null, KeyGesture? keyGesture = null)
        {
            return new MenuItem()
            {
                Header = header,
                Command = command,
                CommandParameter = commandParameter,
                InputGestureText = keyGesture?.GetDisplayStringForCulture(CultureInfo.CurrentCulture)
            };
        }

        #endregion ContextMenu
    }


    public class FileContentToDetailConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FileContent content)
            {
                //var nameText = ResourceService.GetString("@Item.Name") + ": " + content.Name;
                //var sizeText = ResourceService.GetString("@Item.Size") + ": " + (content.Size >= 0 ? $"{(content.Size + 1024 - 1) / 1024:#,0} KB" : "--");
                //var dateText = ResourceService.GetString("@Item.LastWriteTime") + ": " + content. LastWriteTime.ToString(SearchDateTimeTools.DateTimeFormat);
                //var folderText = ResourceService.GetString("@Item.Folder") + ": " + content.DirectoryName;

                var nameText = content.Name;
                var sizeText = (content.Size >= 0 ? $"{(content.Size + 1024 - 1) / 1024:#,0} KB" : "--");
                var dateText = content.LastWriteTime.ToString(SearchDateTimeTools.DateTimeFormat);
                var folderText = content.DirectoryName;
                return nameText + "\n" + sizeText + "\n" + dateText + "\n" + folderText;
            }
        
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
