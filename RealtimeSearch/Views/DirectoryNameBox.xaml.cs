using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace NeeLaboratory.RealtimeSearch.Views
{
    /// <summary>
    /// FilenameBox.xaml の相互作用ロジック
    /// </summary>
    public partial class DirectoryNameBox : UserControl
    {
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(DirectoryNameBox), new PropertyMetadata(""));



        public string DefaultDirectory
        {
            get { return (string)GetValue(DefaultDirectoryProperty); }
            set { SetValue(DefaultDirectoryProperty, value); }
        }

        public static readonly DependencyProperty DefaultDirectoryProperty =
            DependencyProperty.Register(nameof(DefaultDirectory), typeof(string), typeof(DirectoryNameBox), new PropertyMetadata(""));


        public bool IsValid
        {
            get { return (bool)GetValue(IsValidProperty); }
            set { SetValue(IsValidProperty, value); }
        }

        public static readonly DependencyProperty IsValidProperty =
            DependencyProperty.Register(nameof(IsValid), typeof(bool), typeof(DirectoryNameBox), new PropertyMetadata(false));


        public bool SelectDirectory
        {
            get { return (bool)GetValue(SelectDirectoryProperty); }
            set { SetValue(SelectDirectoryProperty, value); }
        }

        public static readonly DependencyProperty SelectDirectoryProperty =
            DependencyProperty.Register(nameof(SelectDirectory), typeof(bool), typeof(DirectoryNameBox), new PropertyMetadata(true));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(DirectoryNameBox), new PropertyMetadata(null));


        public string Filter
        {
            get { return (string)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }

        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register(nameof(Filter), typeof(string), typeof(DirectoryNameBox), new PropertyMetadata(null));


        public string Note
        {
            get { return (string)GetValue(NoteProperty); }
            set { SetValue(NoteProperty, value); }
        }

        public static readonly DependencyProperty NoteProperty =
            DependencyProperty.Register(nameof(Note), typeof(string), typeof(DirectoryNameBox), new PropertyMetadata("フォルダのパスを入力してください"));




        public DirectoryNameBox()
        {
            InitializeComponent();
        }

        private void ButtonOpenDialog_Click(object sender, RoutedEventArgs e)
        {
            if (SelectDirectory)
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = Title ?? "フォルダ選択",
                    SelectedPath = Text
                };

                if (string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    dialog.SelectedPath = DefaultDirectory;
                }

                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    Text = dialog.SelectedPath;
                }
            }
            else
            {
                var dialog = new System.Windows.Forms.OpenFileDialog
                {
                    Title = Title ?? "ファイル選択"
                };
                if (System.IO.File.Exists(Text))
                {
                    dialog.InitialDirectory = System.IO.Path.GetDirectoryName(Text);
                }
                //dialog.FileName = Text;
                dialog.Filter = Filter;

                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    Text = dialog.FileName;
                }
            }
        }

        private void PathTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
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

        private void PathTextBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is not string[] dropFiles) return;

            if (SelectDirectory)
            {
                if (Directory.Exists(dropFiles[0]))
                {
                    Text = dropFiles[0];
                }
                else
                {
                    Text = Path.GetDirectoryName(dropFiles[0]) ?? "";
                }
            }
            else
            {
                Text = dropFiles[0];
            }
        }
    }

}