﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Interop;
using System.Runtime.InteropServices;

/*
 * - [v] 右クリックでファイルを開く 
 * - [v] 右クリックでファイルの場所を開く
 * - [v] 右クリックでファイル名をコピー
 * - [v] 初期化時の検索予約
 * - [v] 検索用文字列の保存(大文字、小文字、全角、半角、ひら、カタの統一)
 * - [v] 検索の非同期化
 * -- [v] 設定の保存/読み込み
 * -- [v] デフォルト設定
 * -- [v] 検索パスの設定
 * -- [v] クリップボードの監視機能ON/OFF
 * -- [x] ウィンドウアクティブON/OFF
 * - [v] ENTERで検索開始
 * - [v] 検索キーワード改行無効
 * 
 * [余裕があれば]
 * - [v] ICommandの引数
 * - [x] 過去のキーワード履歴
 * - [v] 検索ボタンをエクスプローラー風にする
 * - [v] リスト項目の外部へのファイルドラッグ
 * - [v] ファイルアイコンの表示
 * - [v] ファイルの種類の表示
 * - [v] ファイルサイズの表示
 * - [v] 更新日の表示
 * - [v] 項目によるソート
 * - [] フォルダの状態を監視して変更があれば自動的にインデックスを作り直す
 * - [] 記号文字の有効/無効
 * - [] 多重起動チェック
 * - [] マルチ選択ドラッグ
 * - [] リアルタイムサーチ
 * 
 * ///
 * アイコン作成ツールほしい
 */

namespace RealtimeSearch
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowVM VM;

        //
        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            // タイトルに[Debug]を入れる
            //this.Title += " [Debug]";
#endif
            VM = new MainWindowVM();
            this.DataContext = VM;

            RegistRoutedCommand();
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


        Point start;
        ListViewItem downed;


        // ファイルのドラッグ判定開始
        private void PreviewMouseDown_Event(object sender, MouseButtonEventArgs e)        
        {
            downed = sender as ListViewItem;
            start = e.GetPosition(downed);
        }

        // ファイルのドラッグ開始
        private void PreviewMouseMove_Event(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var s = sender as ListViewItem;
            var pn = s.Content as File;


            if (downed != null && downed == s && e.LeftButton == MouseButtonState.Pressed)
            {
                var current = e.GetPosition(s);
                if (Math.Abs(current.X - start.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(current.Y - start.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    downed = null;

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



        //public static readonly ICommand OpenCommand = new RoutedCommand("OpenCommand", typeof(MainWindow));
        public static readonly RoutedCommand OpenCommand = new RoutedCommand();
        public static readonly ICommand OpenPlaceCommand = new RoutedCommand("OpenPlaceCommand", typeof(MainWindow));
        public static readonly ICommand CopyNameCommand = new RoutedCommand("CopyNameCommand", typeof(MainWindow));
        public static readonly RoutedCommand RenameCommand = new RoutedCommand();

        void RegistRoutedCommand()
        {
            OpenCommand.InputGestures.Add(new KeyGesture(Key.Enter, ModifierKeys.None, "Enter"));
            var openCommandBinding = new CommandBinding(OpenCommand, Open_Executed);
            listView01.CommandBindings.Add(openCommandBinding);

            var openPlaceCommandBinding = new CommandBinding(OpenPlaceCommand, OpenPlace_Executed);
            listView01.CommandBindings.Add(openPlaceCommandBinding);

            var copyNameCommandBinding = new CommandBinding(CopyNameCommand, CopyName_Executed);
            listView01.CommandBindings.Add(copyNameCommandBinding);

            RenameCommand.InputGestures.Add(new KeyGesture(Key.F2, ModifierKeys.None, "F2"));
            var renameCommandBinding = new CommandBinding(RenameCommand, Rename_Executed);
            listView01.CommandBindings.Add(renameCommandBinding);
        }

        // 名前の変更
        void Rename_Executed(object target, ExecutedRoutedEventArgs e)
        {
            File file = (target as ListView)?.SelectedItem as File;
            if (file != null)
            {
                if (!System.IO.File.Exists(file.Path) && !System.IO.Directory.Exists(file.Path))
                {
                    MessageBox.Show($"{file.Path} が見つかりません。", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dialog = new RenameWindow(file);
                dialog.Owner = this;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dialog.ShowDialog();
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
                    Process.Start(file.Path);
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
                System.Windows.Clipboard.SetDataObject(text);
            }
        }




        //// リストのソート用

        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        void GridViewColumnHeader_ClickHandler(object sender, RoutedEventArgs e)
        {
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

                    //string header = headerClicked.Column.Header as string;
                    //string header = headerClicked.Column.Header.ToString();

                    Binding binding = headerClicked.Column.DisplayMemberBinding as Binding;

                    string header = binding != null
                        ? (headerClicked.Column.DisplayMemberBinding as Binding).Path.Path
                        : headerClicked.Tag as string;
                    //string header = (headerClicked.Column.DisplayMemberBinding as Binding).Path.Path;

                    //string header = headerClicked.Tag as string;
                    Sort(header, direction);

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

        private void Sort(string sortBy, ListSortDirection direction)
        {
            //ICollectionView dataView = CollectionViewSource.GetDefaultView(fileList.ItemsSource);

            ListCollectionView dataView = CollectionViewSource.GetDefaultView(listView01.ItemsSource) as ListCollectionView;

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }



        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new SettingWindow();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.DataContext = VM;
            window.ShowDialog();

#if false
            if (this.SettingControl.Visibility == Visibility.Visible)
            {
                this.SettingControl.Visibility = Visibility.Hidden;
            }
            else
            {
                this.SettingControl.Visibility = Visibility.Visible;
            }
#endif
        }

    }
}
