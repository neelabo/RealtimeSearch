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
using System.Windows.Shapes;

namespace RealtimeSearch
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfigWindow : Window
    {
        ConfigViewModel vm;

        //----------------------------------------------------------------------------
        public ConfigWindow(ConfigViewModel vm)
        {
            InitializeComponent();

            this.vm = vm;
            this.DataContext = this.vm;
        }

        //----------------------------------------------------------------------------
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        //----------------------------------------------------------------------------
        private void AddSearchPath(string path)
        {
            if (System.IO.File.Exists(path))
            {
                path = System.IO.Path.GetDirectoryName(path);
            }

            vm.AddSearchPath(path);

            // TODO: 追加した項目を選択状態にする
        }

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

            if (dlg.ShowDialog(this) == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                AddSearchPath(dlg.FileName);
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

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ButtonDel_Click(object sender, RoutedEventArgs e)
        {
            // 項目の削除
            if (this.listBox1.SelectedIndex >= 0)
            {
                vm.RemoveSearchPath((string)this.listBox1.SelectedItem);
            }
        }

        //
        void New_Executed(object target, ExecutedRoutedEventArgs e)
        {
            vm.New();
            this.DataContext = vm;
        }

        //
        void Open_Executed(object target, ExecutedRoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FilterIndex = 1;
            dialog.Filter = "設定 ファイル(.yaml)|*.yaml|All Files(*.*)|*.*";
            dialog.InitialDirectory = vm.GetConfigDirectory();
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                vm.Load(dialog.FileName);
                this.DataContext = vm;
            }
        }

        //
        void Save_Executed(object target, ExecutedRoutedEventArgs e)
        {
            vm.Save(vm.Config.Path);
        }

        //
        void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrEmpty(vm.Config.Path);
        }          

        void SaveAs_Executed(object target, ExecutedRoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.FilterIndex = 1;
            dialog.Filter = "設定 ファイル(.yaml)|*.yaml|All Files(*.*)|*.*";
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                vm.Save(dialog.FileName);
            }
        }


    }
}
