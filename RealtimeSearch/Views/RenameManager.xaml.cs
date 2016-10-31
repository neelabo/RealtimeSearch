﻿using System;
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

namespace RealtimeSearch
{
    /// <summary>
    /// RenameManager.xaml の相互作用ロジック
    /// </summary>
    public partial class RenameManager : UserControl
    {
        public RenameManager()
        {
            InitializeComponent();
        }

        public void Open(RenameControl rename)
        {
            rename.Close += Rename_Close;

            var pos = rename.Target.TranslatePoint(new Point(0, 0), this);
            Canvas.SetLeft(rename, pos.X - 3);
            Canvas.SetTop(rename, pos.Y - 2);

            this.Root.Children.Add(rename);

            rename.Target.Visibility = Visibility.Hidden;
        }

        private void Rename_Close(object sender, EventArgs e)
        {
            var rename = (RenameControl)sender;
            rename.Target.Visibility = Visibility.Visible;

            this.Root.Children.Remove(sender as RenameControl);
        }

        public void Stop()
        {
            if (this.Root.Children != null && this.Root.Children.Count > 0)
            {
                var renames = this.Root.Children.OfType<RenameControl>().ToList();
                foreach (var rename in renames)
                {
                    rename.Stop(true);
                }
            }
        }
    }
}
