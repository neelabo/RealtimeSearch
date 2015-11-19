using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace RealtimeSearch
{
    /// <summary>
    /// Setting.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingControl : UserControl, INotifyPropertyChanged
    {
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion
    
        //----------------------------------------------------------------------------
        public static readonly DependencyProperty SettingProperty = DependencyProperty.Register(
                "Setting",
                typeof(Setting),
                typeof(SettingControl),
                new FrameworkPropertyMetadata(
                        new PropertyChangedCallback(SettingControl.OnSettingChanged)
                )
        );

        public Setting Setting
        {
            get { return (Setting)GetValue(SettingProperty); }
            set { SetValue(SettingProperty, value); UpdateCollectionViewSource(); }
        }

        private static void OnSettingChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            SettingControl thisCtrl = (SettingControl)obj;
            thisCtrl.UpdateCollectionViewSource();
        }


        //public CollectionViewSource CollectionViewSource { get; set; }
        #region Property: CollectionViewSource
        private CollectionViewSource _CollectionViewSource;
        public CollectionViewSource CollectionViewSource
        {
            get { return _CollectionViewSource; }
            set { _CollectionViewSource = value; OnPropertyChanged(); }
        }
        #endregion


        public CollectionViewSource CollectionViewSource2 { get; set; }
        public string[] CollectionViewSource3 { get; set; }


    //----------------------------------------------------------------------------
    public SettingControl()
        {
            CollectionViewSource3 = new string[] { "AAA", "BBB", "CCC" };

            CollectionViewSource2 = new CollectionViewSource();
            CollectionViewSource2.Source = new ObservableCollection<string>(CollectionViewSource3);

            InitializeComponent();

            BaseControl.DataContext = this;

        }


        private void UpdateCollectionViewSource()
        {
            var collectionViewSource = new CollectionViewSource();
            collectionViewSource.Source = Setting.SearchPaths;
            collectionViewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription(null, System.ComponentModel.ListSortDirection.Ascending));

            //collectionViewSource.Source = new string[] { "AAA", "BBB", "CCC" };
            CollectionViewSource = collectionViewSource;
        }


        //----------------------------------------------------------------------------
        private void AddSearchPath(string path)
        {
            if (System.IO.File.Exists(path))
            {
                path = System.IO.Path.GetDirectoryName(path);
            }

            //vm.AddSearchPath(path);
            Setting.SearchPaths.Add(path);

            // TODO: 追加した項目を選択状態にする
        }


        //----------------------------------------------------------------------------


        //----------------------------------------------------------------------------
        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            // フォルダ選択
            var dlg = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();
            dlg.Title = "作業フォルダの選択";
            dlg.IsFolderPicker = true;
            //dlg.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.DefaultDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog(Window.GetWindow(this)) == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                AddSearchPath(dlg.FileName);
            }
        }

        private void ButtonDel_Click(object sender, RoutedEventArgs e)
        {
            // 項目の削除
            if (this.listBox1.SelectedIndex >= 0)
            {
                //vm.RemoveSearchPath((string)this.listBox1.SelectedItem);
                Setting.SearchPaths.Remove((string)this.listBox1.SelectedItem);
            }
        }



        private void ListBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, true))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            var dropFiles = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            if (dropFiles == null) return;

            AddSearchPath(dropFiles[0]);
        }

        private void SettingControl_Loaded(object sender, RoutedEventArgs e)
        {
            /*
            var collectionViewSource = new CollectionViewSource();
            collectionViewSource.Source = Setting.SearchPaths;
            collectionViewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription(null, System.ComponentModel.ListSortDirection.Ascending));
            */
        }
    }
}
