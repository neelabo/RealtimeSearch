using NeeLaboratory.RealtimeSearch.Models;
using NeeLaboratory.RealtimeSearch.TextResource;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;


namespace NeeLaboratory.RealtimeSearch.Views
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window
    {
        public static readonly RoutedCommand CloseCommand = new("CloseCommand", typeof(SettingWindow), [new KeyGesture(Key.Escape)]);


        public SettingWindow()
        {
            InitializeComponent();
        }


        public SettingWindow(AppSettings setting, int index) : this()
        {
            this.NavicationView.ItemsSource = new List<NavigationItem>
            {
                new NavigationItem(ResourceService.GetString("@Setting.General"), "\uE713", new SettingGeneralControl(setting)),
                new NavigationItem(ResourceService.GetString("@Setting.SearchArea"), "\uE8B7", new SettingSearchAreaControl(setting)),
                new NavigationItem(ResourceService.GetString("@Setting.ExternalApp"), "\uE8A9", new SettingExternalAppControl(setting)),
                new NavigationItem(ResourceService.GetString("@Setting.Help"), "\uE9CE", new SettingAboutControl()),
            };

            this.NavicationView.SelectedIndex = index;
        }

        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }
    }
}
