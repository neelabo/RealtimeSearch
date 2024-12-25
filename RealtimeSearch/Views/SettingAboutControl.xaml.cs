using NeeLaboratory.RealtimeSearch.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// SettingAboutControl.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingAboutControl : UserControl
    {
        public SettingAboutControl()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Debug.WriteLine($"{e.Uri.OriginalString}");

            switch (e.Uri.OriginalString)
            {
                case "License":
                    OpenManual(0);
                    break;
                case "SearchOptions":
                    OpenManual(1);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private void OpenManual(int id)
        {
            // help command
            var readmeUri = "file://" + AppModel.AppInfo.AssemblyLocation.Replace('\\', '/').TrimEnd('/') + $"/README.html";

            try
            {
                var startInfo = new ProcessStartInfo(readmeUri) { UseShellExecute = true };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show( $"{ex.Message}", "ヘルプを開けませんでした", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
