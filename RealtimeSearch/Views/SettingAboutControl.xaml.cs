using NeeLaboratory.RealtimeSearch.Models;
using NeeLaboratory.RealtimeSearch.Services;
using NeeLaboratory.RealtimeSearch.TextResource;
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

            this.VersionTextBlock.Text = "version " + ApplicationInfo.Current.ProductVersion + " (64bit)";
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Debug.WriteLine($"{e.Uri.OriginalString}");
            OpenManual(e.Uri.OriginalString);
        }

        private void OpenManual(string fileName)
        {
            if (TextResources.Culture.Name == "ja")
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(fileName);
                var ext = System.IO.Path.GetExtension(fileName);
                fileName = name + ".ja-jp" + ext;
            }

            var readmeUri = System.IO.Path.Combine(ApplicationInfo.Current.AssemblyLocation, fileName);

            try
            {
                var startInfo = new ProcessStartInfo(readmeUri) { UseShellExecute = true };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Hyperlink_RequestWeb(object sender, RequestNavigateEventArgs e)
        {
            Debug.WriteLine($"{e.Uri.OriginalString}");
            try
            {
                var startInfo = new ProcessStartInfo(e.Uri.OriginalString) { UseShellExecute = true };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
