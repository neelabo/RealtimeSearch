using Microsoft.Win32;
using NeeLaboratory.IO.Search.Files;
using NeeLaboratory.RealtimeSearch.Models;
using NeeLaboratory.RealtimeSearch.TextResource;
using NeeLaboratory.RealtimeSearch.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NeeLaboratory.RealtimeSearch.Views
{
    /// <summary>
    /// SettingSearchAreaControl.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingSearchAreaControl : UserControl
    {
        public static readonly RoutedCommand AddCommand = new("AddCommand", typeof(SettingSearchAreaControl), [new KeyGesture(Key.Insert)]);
        public static readonly RoutedCommand DelCommand = new("DelCommand", typeof(SettingSearchAreaControl), [new KeyGesture(Key.Delete)]);

        private readonly SettingSearchAreaViewModel? _vm;


        public SettingSearchAreaControl()
        {
            InitializeComponent();
        }

        public SettingSearchAreaControl(AppSettings setting)
        {
            InitializeComponent();
            _vm = new SettingSearchAreaViewModel(setting);
            this.DataContext = _vm;
        }


        private void AddCommand_Executed(object target, ExecutedRoutedEventArgs e)
        {
            if (_vm is null) return;

            var dialog = new OpenFolderDialog()
            {
                Title = ResourceService.GetString("@Setting.AddSearchFolder"),
                InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal)
            };


            var result = dialog.ShowDialog();
            if (result == true)
            {
                _vm.AddSearchPath(dialog.FolderName);
            }
        }

        private void DelCommand_Executed(object target, ExecutedRoutedEventArgs e)
        {
            if (_vm is null) return;

            if (e.Parameter is FileArea item)
            {
                _vm.RemoveSearchPath(item);
            }
            else if (target is ListBox listBox && listBox.SelectedItem is FileArea selectedItem)
            {
                _vm.RemoveSearchPath(selectedItem);
            }
        }

        private void ListBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (_vm is null) return;
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
            if (_vm is null) return;
            if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is not string[] dropFiles) return;

            foreach (var file in dropFiles)
            {
                _vm.AddSearchPath(file);
            }
        }
    }
}
