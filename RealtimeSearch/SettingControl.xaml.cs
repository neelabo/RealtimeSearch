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
                new FrameworkPropertyMetadata(new PropertyChangedCallback(SettingControl.OnSettingChanged))
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

        #region Property: SelectedPath
        private string _SelectedPath;
        public string SelectedPath
        {
            get { return _SelectedPath; }
            set { _SelectedPath = value; OnPropertyChanged(); }
        }
        #endregion

        public static readonly RoutedCommand AddCommand = new RoutedCommand("AddCommand", typeof(SettingControl));
        public static readonly RoutedCommand DelCommand = new RoutedCommand("DelCommand", typeof(SettingControl));

        //----------------------------------------------------------------------------
        public SettingControl()
        {
            InitializeComponent();

            BaseControl.DataContext = this;

            AddCommand.InputGestures.Add(new KeyGesture(Key.Insert, ModifierKeys.None, "Ins"));
            listBox1.CommandBindings.Add(new CommandBinding(AddCommand, AddCommand_Executed));

            DelCommand.InputGestures.Add(new KeyGesture(Key.Delete, ModifierKeys.None, "Del"));
            listBox1.CommandBindings.Add(new CommandBinding(DelCommand, DelCommand_Executed));
        }

        private void AddCommand_Executed(object target, ExecutedRoutedEventArgs e)
        {
            AddPathWithDialog();
        }

        private void DelCommand_Executed(object target, ExecutedRoutedEventArgs e)
        {
            if (SelectedPath != null)
            {
                Setting.SearchPaths.Remove(SelectedPath);
                SelectedPath = null;
            }
        }

        private void DelCommand_CanExecute(object target, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = SelectedPath != null;
        }


        private void UpdateCollectionViewSource()
        {
            var collectionViewSource = new CollectionViewSource();
            collectionViewSource.Source = Setting.SearchPaths;
            collectionViewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription(null, System.ComponentModel.ListSortDirection.Ascending));

            //collectionViewSource.Source = new string[] { "AAA", "BBB", "CCC" };
            CollectionViewSource = collectionViewSource;
            SelectedPath = null;
        }


        //----------------------------------------------------------------------------
        private void AddSearchPath(string path)
        {
            if (!System.IO.Directory.Exists(path)) return;

            string existPath = Setting.SearchPaths.FirstOrDefault(p => p == path);

            if (existPath != null)
            {
                SelectedPath = existPath;
                return;
            }

            Setting.SearchPaths.Add(path);
            SelectedPath = path;
        }


        //----------------------------------------------------------------------------

#if false
        //----------------------------------------------------------------------------
        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            AddPathWithDialog();
        }
#endif

        private void AddPathWithDialog()
        { 
            // フォルダ選択
            var dlg = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();
            dlg.Title = "検索フォルダの追加";
            dlg.IsFolderPicker = true;
            if (SelectedPath != null) dlg.InitialDirectory = SelectedPath;
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

#if false
        private void ButtonDel_Click(object sender, RoutedEventArgs e)
        {
            // 項目の削除
            if (this.listBox1.SelectedIndex >= 0)
            {
                //vm.RemoveSearchPath((string)this.listBox1.SelectedItem);
                Setting.SearchPaths.Remove((string)this.listBox1.SelectedItem);
            }
        }
#endif



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

            foreach (var file in dropFiles)
            {
                AddSearchPath(file);
            }
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
