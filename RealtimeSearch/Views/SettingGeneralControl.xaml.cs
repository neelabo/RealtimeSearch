using NeeLaboratory.RealtimeSearch.Models;
using NeeLaboratory.RealtimeSearch.ViewModels;
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

namespace NeeLaboratory.RealtimeSearch.Views
{
    /// <summary>
    /// SettingGeneralControl.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingGeneralControl : UserControl
    {
        public SettingGeneralControl()
        {
            InitializeComponent();
        }

        public SettingGeneralControl(AppSettings setting)
        {
            InitializeComponent();
            this.DataContext = new SettingGeneralViewModel(setting);
        }
    }
}
