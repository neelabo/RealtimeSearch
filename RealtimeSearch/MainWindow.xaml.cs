// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

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

namespace RealtimeSearch
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowVM VM;

        public MainWindow()
        {
            InitializeComponent();

            VM = new MainWindowVM();
            this.DataContext = VM;

            RegistRoutedCommand();

            VM.PropertyChanged += MainWindowVM_PropertyChanged;
            VM.StateMessageChanged += MainWindowVM_StateMessageChanged;
        }

        private void MainWindowVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VM.Files))
            {
                Dispatcher.BeginInvoke(new Action(GridViewColumnHeader_Reset), null);
            }
        }

        private void MainWindowVM_StateMessageChanged(object sender, SearchEngineState e)
        {
            DispBusy(e == SearchEngineState.Search);
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            // 設定読み込み
            VM.Open();

            // ウィンドウ座標復元
            VM.RestoreWindowPlacement(this);

            // ListViewレイアウト復元
            RestoreListViewMemento(VM.Setting.ListViewColumnMemento);
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // クリップボード監視開始
            VM.StartClipboardMonitor(this);

            // 検索パスが設定されていなければ設定画面を開く
            if (VM.Setting.SearchPaths.Count <= 0)
            {
                ShowSettingWindow();
            }
        }


        private void Window_Closing(object sender, EventArgs e)
        {
            // ListViewレイアウト保存
            VM.Setting.ListViewColumnMemento = CreateListViewMemento();

            // ウィンドウ座標保存
            VM.StoreWindowPlacement(this);
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            // 設定保存
            VM.Close();
        }


        //
        void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            File file = ((ListViewItem)sender).Content as File;
            if (file == null) return;

            Execute(file);
        }

        //
        private void Execute(File file)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(VM.Setting.ExternalApplication))
                {
                    System.Diagnostics.Process.Start(file.Path);
                }
                else
                {
                    var commandName = VM.Setting.ExternalApplication;
                    var arguments = VM.Setting.ExternalApplicationParam.Replace("$(file)", file.Path);
                    System.Diagnostics.Process.Start(commandName, arguments);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                MessageBox.Show(e.Message, "実行失敗", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void keyword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                VM.Search();
            }
        }


        // ドラッグ用
        Point _DragStart;
        ListViewItem _DragDowned;

        // ファイルのドラッグ判定開始
        private void PreviewMouseDown_Event(object sender, MouseButtonEventArgs e)
        {
            _DragDowned = sender as ListViewItem;
            _DragStart = e.GetPosition(_DragDowned);
        }

        // ファイルのドラッグ開始
        private void PreviewMouseMove_Event(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var s = sender as ListViewItem;
            var pn = s.Content as File;

            if (_DragDowned != null && _DragDowned == s && e.LeftButton == MouseButtonState.Pressed)
            {
                var current = e.GetPosition(s);
                if (Math.Abs(current.X - _DragStart.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(current.Y - _DragStart.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _DragDowned = null;

                    if (listView01.SelectedItems.Count > 0)
                    {
                        string[] paths = new string[listView01.SelectedItems.Count];
                        for (int i = 0; i < listView01.SelectedItems.Count; ++i)
                        {
                            paths[i] = ((File)listView01.SelectedItems[i]).Path;
                        }

                        DataObject data = new DataObject();
                        data.SetData(DataFormats.FileDrop, paths);
                        DragDrop.DoDragDrop(s, data, DragDropEffects.Copy);
                    }
                }
            }
        }



        public static readonly RoutedCommand OpenCommand = new RoutedCommand("OpenCommand", typeof(MainWindow));
        public static readonly RoutedCommand CopyCommand = new RoutedCommand("CopyCommand", typeof(MainWindow));
        public static readonly RoutedCommand OpenPlaceCommand = new RoutedCommand("OpenPlaceCommand", typeof(MainWindow));
        public static readonly RoutedCommand CopyNameCommand = new RoutedCommand("CopyNameCommand", typeof(MainWindow));
        public static readonly RoutedCommand RenameCommand = new RoutedCommand("RenameCommand", typeof(MainWindow));
        public static readonly RoutedCommand DeleteCommand = new RoutedCommand("DeleteCommand", typeof(MainWindow));

        public static readonly RoutedCommand SearchCommand = new RoutedCommand("SearchCommand", typeof(MainWindow));
        public static readonly RoutedCommand WebSearchCommand = new RoutedCommand("WebSearchCommand", typeof(MainWindow));

        void RegistRoutedCommand()
        {
            OpenCommand.InputGestures.Add(new KeyGesture(Key.Enter));
            listView01.CommandBindings.Add(new CommandBinding(OpenCommand, Open_Executed));

            CopyCommand.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control));
            listView01.CommandBindings.Add(new CommandBinding(CopyCommand, Copy_Executed));

            listView01.CommandBindings.Add(new CommandBinding(OpenPlaceCommand, OpenPlace_Executed));

            CopyNameCommand.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift));
            listView01.CommandBindings.Add(new CommandBinding(CopyNameCommand, CopyName_Executed));

            RenameCommand.InputGestures.Add(new KeyGesture(Key.F2));
            listView01.CommandBindings.Add(new CommandBinding(RenameCommand, Rename_Executed));

            DeleteCommand.InputGestures.Add(new KeyGesture(Key.Delete));
            listView01.CommandBindings.Add(new CommandBinding(DeleteCommand, Delete_Executed));

            SearchCommand.InputGestures.Add(new KeyGesture(Key.F5));
            SearchCommand.InputGestures.Add(new KeyGesture(Key.R, ModifierKeys.Control));
            this.CommandBindings.Add(new CommandBinding(SearchCommand, Search_Executed));

            WebSearchCommand.InputGestures.Add(new KeyGesture(Key.F, ModifierKeys.Control));
            this.CommandBindings.Add(new CommandBinding(WebSearchCommand, WebSearch_Executed));
        }

        // 
        void Search_Executed(object target, ExecutedRoutedEventArgs e)
        {
            VM.Search();
        }

        //
        void WebSearch_Executed(object target, ExecutedRoutedEventArgs e)
        {
            VM.WebSearch();
        }

        // ファイル削除
        void Delete_Executed(object target, ExecutedRoutedEventArgs e)
        {
            var items = (target as ListView)?.SelectedItems;
            if (items != null && items.Count >= 1)
            {
                var message = (items.Count == 1)
                    ? $"このファイルをごみ箱に移動しますか？\n\n{((File)items[0]).Path}"
                    : $"これらの {items.Count} 個の項目をごみ箱に移しますか？";

                var result = MessageBox.Show(message, "削除確認", MessageBoxButton.OKCancel, MessageBoxImage.Question);


                if (result == MessageBoxResult.OK)
                {
                    try
                    {
                        // ゴミ箱に捨てる
                        foreach (var item in items)
                        {
                            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(((File)item).Path, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"ファイル削除に失敗しました\n\n原因: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // 名前の変更
        void Rename_Executed(object target, ExecutedRoutedEventArgs e)
        {
            File file = (target as ListView)?.SelectedItem as File;
            if (file != null)
            {
                if (!System.IO.File.Exists(file.Path) && !System.IO.Directory.Exists(file.Path))
                {
                    MessageBox.Show($"{file.Path} が見つかりません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                VM.IsEnableClipboardListner = false;
                try
                {
                    var dialog = new RenameWindow(file);
                    dialog.Owner = this;
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    dialog.ShowDialog();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    VM.IsEnableClipboardListner = true;
                }
            }
        }


        // ファイルを開く
        void Open_Executed(object target, ExecutedRoutedEventArgs e)
        {
            var items = (target as ListView)?.SelectedItems;

            if (items == null || items.Count > 10) return;

            foreach (var item in items)
            {
                File file = item as File;
                if (file != null && (System.IO.File.Exists(file.Path) || System.IO.Directory.Exists(file.Path)))
                {
                    Execute(file);
                }
            }
        }


        // ファイルの場所を開く
        void OpenPlace_Executed(object target, ExecutedRoutedEventArgs e)
        {
            var items = (target as ListView)?.SelectedItems;

            if (items == null || items.Count > 10) return;

            foreach (var item in items)
            {
                File file = item as File;
                if (file != null && (System.IO.File.Exists(file.Path) || System.IO.Directory.Exists(file.Path)))
                {
                    Process.Start("explorer.exe", "/select,\"" + file.Path + "\"");
                }
            }
        }


        // ファイルのコピー
        void Copy_Executed(object target, ExecutedRoutedEventArgs e)
        {
            var files = new System.Collections.Specialized.StringCollection();

            foreach (var item in (target as ListView)?.SelectedItems)
            {
                File file = item as File;
                if (file != null) files.Add(file.Path);
            }

            if (files.Count > 0) Clipboard.SetFileDropList(files);
        }


        // ファイル名のコピー
        void CopyName_Executed(object target, ExecutedRoutedEventArgs e)
        {
            File file = (target as ListView)?.SelectedItem as File;
            if (file != null)
            {
                string text = System.IO.Path.GetFileNameWithoutExtension(file.Path);
                VM.SetClipboard(text);
                //System.Windows.Clipboard.SetDataObject(text);
            }
        }


        #region リストのソート

        // リストのソート用
        GridViewColumnHeader _LastHeaderClicked = null;
        ListSortDirection _LastDirection = ListSortDirection.Ascending;

        private void GridViewColumnHeader_Reset()
        {
            if (_LastHeaderClicked != null)
            {
                _LastHeaderClicked.Column.HeaderTemplate = null;
                _LastHeaderClicked = null;
            }
            _LastDirection = ListSortDirection.Ascending;
        }

        private void GridViewColumnHeader_ClickHandler(object sender, RoutedEventArgs e)
        {
            if (listView01.ItemsSource == null) return;

            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _LastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_LastDirection == ListSortDirection.Ascending)
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
                    if (_LastHeaderClicked != null && _LastHeaderClicked != headerClicked)
                    {
                        _LastHeaderClicked.Column.HeaderTemplate = null;
                    }

                    _LastHeaderClicked = headerClicked;
                    _LastDirection = direction;
                }
            }
        }

        private void GridViewColumnHeader_Sort(string sortBy, ListSortDirection direction)
        {
            ListCollectionView dataView = CollectionViewSource.GetDefaultView(listView01.ItemsSource) as ListCollectionView;

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        #endregion


        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingWindow();
        }

        private void ShowSettingWindow()
        {
            var window = new SettingWindow(VM.Setting);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }


        /// <summary>
        /// Busy表示/非表示
        /// </summary>
        /// <param name="isDisp"></param>
        private void DispBusy(bool isDisp)
        {
            if (isDisp)
            {
                this.BusyMark.Opacity = 0.0;

                var ani = new DoubleAnimation(0, 0.8, TimeSpan.FromSeconds(0.5));
                ani.BeginTime = TimeSpan.FromSeconds(1.0);
                this.BusyMark.BeginAnimation(UIElement.OpacityProperty, ani);

                var aniRotate = new DoubleAnimation();
                aniRotate.By = 360;
                aniRotate.Duration = TimeSpan.FromSeconds(2.0);
                aniRotate.RepeatBehavior = RepeatBehavior.Forever;
                this.BusyMarkAngle.BeginAnimation(RotateTransform.AngleProperty, aniRotate);
            }
            else
            {
                var ani = new DoubleAnimation(0, TimeSpan.FromSeconds(0.5));
                this.BusyMark.BeginAnimation(UIElement.OpacityProperty, ani, HandoffBehavior.Compose);

                var aniRotate = new DoubleAnimation();
                aniRotate.By = 90;
                aniRotate.Duration = TimeSpan.FromSeconds(0.5);
                this.BusyMarkAngle.BeginAnimation(RotateTransform.AngleProperty, aniRotate);
            }
        }


        #region ListViewColumnMemento

        // カラムヘッダ文字列取得
        private string GetColumnHeaderText(GridViewColumn column)
        {
            return (column.Header as string) ?? (column.Header as GridViewColumnHeader)?.Content as string;
        }

        // リストビューカラム状態保存
        private List<ListViewColumnMemento> CreateListViewMemento()
        {
            var columns = (this.listView01.View as GridView)?.Columns;
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

            var columns = (this.listView01.View as GridView)?.Columns;
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


        // for Debug
        private int _LogCount;

        [Conditional("DEBUG")]
        public void Log(string format, params object[] args)
        {
            if (this.DebugInfo.Visibility != Visibility.Visible) return;
            this.LogTextBox.AppendText(string.Format($"\n{++_LogCount}>{format}", args));
            this.LogTextBox.ScrollToEnd();
        }
    }


    public partial class App : Application
    {
        [Conditional("DEBUG")]
        public static void Log(string format, params object[] args)
        {
            var window = (MainWindow)Application.Current.MainWindow;
            window.Log(format, args);
        }
    }
}
