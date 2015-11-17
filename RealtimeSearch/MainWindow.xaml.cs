using System;
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
        ViewModel vm = new ViewModel();

        //ClipboardWatcher clipboardWatcher;
        ClipboardListner ClipboardListner;

        string defaultConfigPath;

        //
        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            // タイトルに[Debug]を入れる
            this.Title += " [Debug]";
#endif
            
            this.DataContext = vm;
        }

        //
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Topmost = true;
        }

        //
        public async void Search()
        {
            // 検索できる状態ならば
            if (vm.CanSearch())
            {
                // 検索
                vm.Status = "検索中...";
                await Task.Run(() => vm.Search());

#if false
                // ウィンドウをアクティブにする
                this.Activate();
                this.Topmost = true;
                this.Topmost = false;
#endif
            }
        }

        //
        public async void clipboardWatcher_DrawClipboard(object sender, System.EventArgs e)
        {
            if (vm.ConfigViewModel.IsMonitorClipboard && Clipboard.ContainsText())
            {
                // どうにも例外(CLIPBRD_E_CANT_OPEN)が発生してしまうのでリトライさせることにした
                RETRY:
                try
                {
                    // キーワード設定
                    vm.Keyword = Clipboard.GetText();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);

                    await Task.Delay(100);
                    goto RETRY;
                }

                // 検索
                Search();
            }
        }

        //
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Properties.Settings.Default.ConfigFile = vm.ConfigViewModel.Config.Path;
            //Properties.Settings.Default.Save();
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            // 強制保存
            vm.ConfigViewModel.Save(defaultConfigPath);

            //this.clipboardWatcher.Dispose();
            ClipboardListner.Dispose();
        }

        //
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            System.Reflection.Assembly myAssembly = System.Reflection.Assembly.GetEntryAssembly();
            defaultConfigPath = System.IO.Path.GetDirectoryName(myAssembly.Location) + "\\Default.yaml";

            // アウト！
            //throw new Exception("HOGE");

            // 設定読み込み
            if (System.IO.File.Exists(defaultConfigPath))
            {
                vm.ConfigViewModel.Load(defaultConfigPath);
                vm.UpdateConfig();
            }

            // クリップボード監視
            //this.clipboardWatcher = new ClipboardWatcher(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            //this.clipboardWatcher.DrawClipboard += clipboardWatcher_DrawClipboard;

            ClipboardListner = new ClipboardListner(this);
            ClipboardListner.ClipboardUpdate += clipboardWatcher_DrawClipboard;

            Research();
        }

        //
        private void buttonResearch_Click(object sender, RoutedEventArgs e)
        {
            Research();
        }

        //
        private async void Research()
        {
            this.buttonResearch.IsEnabled = false;

            // インデックス作成
            await Task.Run(() => vm.GenerateIndex());

            // 必要があれば検索を行う
            Search();

            this.buttonResearch.IsEnabled = true;
        }

        //
        void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            File file = ((ListViewItem)sender).Content as File;
            if (file == null) return;

            System.Diagnostics.Process.Start(file.Path);
        }

        //
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            vm.SetKeyworkdDarty();
            Search();
        }

        private void buttonSetting_Click(object sender, RoutedEventArgs e)
        {
            var svm= new ConfigViewModel(vm.ConfigViewModel.Config.Clone());

            //
            var win = new ConfigWindow(svm);
            win.ShowDialog();
            // vm.UpdateSetting();

            //string hoge = svm.Setting.Serialize();
            //MessageBox.Show(hoge);

            if (svm.IsDarty)
            {
                svm.IsDarty = false;
                //vm.SettingViewModel.Setting = svm.Setting;
                vm.ConfigViewModel = svm;

                this.DataContext = vm; // リバインド
                Research();
            }

            //this.Close();
        }

        private void keyword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                vm.SetKeyworkdDarty();
                Search();
            }
        }

        Point start;
        ListViewItem downed;

                
        private void PreviewMouseDown_Event(object sender, MouseButtonEventArgs e)        
        {
            downed = sender as ListViewItem;
            start = e.GetPosition(downed);
        }

        private void PreviewMouseMove_Event(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //MessageBox.Show("AAA");

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

#if false
                    {
                        DataObject data = new DataObject();
                        string[] paths = { pn.Path };
                        data.SetData(DataFormats.FileDrop, paths);
                        DragDrop.DoDragDrop(s, data, DragDropEffects.Copy);
                    }
                    //DragDrop.DoDragDrop(sender, sender.Content, DragDropEffects.Copy);
#endif
                }

            }

        }

        // コピー
        void Copy_Executed(object target, ExecutedRoutedEventArgs e)
        {
            File file = (e.Parameter ?? this.listView01.SelectedItem) as File;
            if (file != null)
            {
                string text = System.IO.Path.GetFileNameWithoutExtension(file.Path);
                System.Windows.Clipboard.SetDataObject(text);
            }
        }


        ////

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

    }
}
