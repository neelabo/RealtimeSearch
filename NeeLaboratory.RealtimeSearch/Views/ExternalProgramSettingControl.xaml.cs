// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

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

namespace NeeLaboratory.RealtimeSearch
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

        // Using a DependencyProperty as the backing store for Program.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProgramProperty =
            DependencyProperty.Register("Program", typeof(ExternalProgram), typeof(ExternalProgramSettingControl), new PropertyMetadata(null));

        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Header.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(string), typeof(ExternalProgramSettingControl), new PropertyMetadata("外部アプリ設定"));




        public ExternalProgramSettingControl()
        {
            InitializeComponent();

            this.Root.DataContext = this;
        }
    }
}
