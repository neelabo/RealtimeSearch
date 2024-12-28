using NeeLaboratory.IO.Search.Files;
using NeeLaboratory.RealtimeSearch.Models;
using NeeLaboratory.RealtimeSearch.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// SettingExternalAppControl.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingExternalAppControl : UserControl
    {
        public static readonly RoutedCommand AddExternalProgramCommand = new("AddExternalProgramCommand", typeof(SettingExternalAppControl), [new KeyGesture(Key.Insert)]);
        public static readonly RoutedCommand DeleteExternalProgramCommand = new("DeleteExternalProgramCommand", typeof(SettingExternalAppControl), [new KeyGesture(Key.Delete)]);

        private SettingExternalAppViewModel? _vm;


        public SettingExternalAppControl()
        {
            InitializeComponent();
        }

        public SettingExternalAppControl(AppSettings setting)
        {
            InitializeComponent();
            _vm = new SettingExternalAppViewModel(setting);
            this.DataContext = _vm;
        }

        private void AddExternalProgramCommand_Executed(object target, ExecutedRoutedEventArgs e)
        {
            if (_vm is null) return;

            var item = _vm.AddExternalProgram();
            this.ExternalProgramListBox.ScrollIntoView(item);
            this.ExternalProgramListBox.SelectedItem = item;
        }

        private void DeleteExternalProgramCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (_vm is null) return;

            if (e.Parameter is ExternalProgram item)
            {
                _vm.DeleteExternalProgram(item);
            }
            else if (sender is ListBox listBox && listBox.SelectedItem is ExternalProgram selectedItem)
            {
                _vm.DeleteExternalProgram(selectedItem);
            }
        }

        private void ButtonDown_Click(object sender, RoutedEventArgs e)
        {
            if (_vm is null) return;

            var item = this.ExternalProgramListBox.SelectedItem as ExternalProgram;
            if (item is null) return;
            _vm.MoveToDown(item);
        }

        private void ButtonUp_Click(object sender, RoutedEventArgs e)
        {
            if (_vm is null) return;

            var item = this.ExternalProgramListBox.SelectedItem as ExternalProgram;
            if (item is null) return;
            _vm.MoveToUp(item);
        }
    }

}
