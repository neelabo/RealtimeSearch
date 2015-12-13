// Copyright (c) 2015 Mitsuhiro Ito (nee)
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
        }

        private void MainWindowVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VM.Files))
            {
                Dispatcher.BeginInvoke(new Action(GridViewColumnHeader_Reset), null);
            }
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            // 設定読み込み
            VM.Open();

            // ウィンドウ座標復元
            VM.RestoreWindowPlacement(this);
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
            // ウィンドウ座標保存
            VM.StoreWindowPlacement(this);
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            // 設定保存
            VM.Close();
        }


        void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            File file = ((ListViewItem)sender).Content as File;
            if (file == null) return;

            System.Diagnostics.Process.Start(file.Path);
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
        }


        // 名前の変更
        void Rename_Executed(object target, ExecutedRoutedEventArgs e)
        {
            File file = (target as ListView)?.SelectedItem as File;
            if (file != null)
            {
                if (!System.IO.File.Exists(file.Path) && !System.IO.Directory.Exists(file.Path))
                {
                    MessageBox.Show($"{file.Path} が見つかりません。", "通知", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                catch(Exception ex)
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
                    try
                    {
                        Process.Start(file.Path);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
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
