using NeeLaboratory.RealtimeSearch.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// ExternalProgramSettingControl.xaml の相互作用ロジック
    /// </summary>
    public partial class ExternalProgramSettingControl : UserControl
    {
        public ExternalProgram Program
        {
            get { return (ExternalProgram)GetValue(ProgramProperty); }
            set { SetValue(ProgramProperty, value); }
        }

        public static readonly DependencyProperty ProgramProperty =
            DependencyProperty.Register("Program", typeof(ExternalProgram), typeof(ExternalProgramSettingControl), new PropertyMetadata(null));


        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(string), typeof(ExternalProgramSettingControl), new PropertyMetadata("外部アプリ設定"));



        public ExternalProgramSettingControl()
        {
            InitializeComponent();

            this.Root.DataContext = this;
        }
    }

}
