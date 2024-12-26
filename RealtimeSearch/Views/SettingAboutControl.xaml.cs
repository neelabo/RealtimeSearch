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
            OpenManual(e.Uri.OriginalString);
        }

        private void OpenManual(string fileName)
        {
            // TODO: check culture
            if (true)
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(fileName);
                var ext = System.IO.Path.GetExtension(fileName);
                fileName = name + ".ja-jp" + ext;
            }

            var readmeUri = System.IO.Path.Combine(AppModel.AppInfo.AssemblyLocation, fileName);

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

    }
}
