using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace RealtimeSearch
{
    /// <summary>
    /// RenameWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class RenameWindow : Window, INotifyPropertyChanged
    {
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion

        #region Property: File
        private File _File;
        public File File
        {
            get { return _File; }
            set { _File = value; OnPropertyChanged(); NewName = System.IO.Path.GetFileName(_File.Path); }
        }
        #endregion

        #region Property: NewName
        private string _NewName;
        public string NewName
        {
            get { return _NewName; }
            set { _NewName = value; OnPropertyChanged(); }
        }
        #endregion

        public RenameWindow(File file)
        {
            this.File = file;

            InitializeComponent();
            InitializeCommand();

            this.DataContext = this;

        }

        public static readonly RoutedCommand CloseCommand = new RoutedCommand();
        public static readonly RoutedCommand OkCommand = new RoutedCommand();

        void InitializeCommand()
        {
            // ok command
            OkCommand.InputGestures.Add(new KeyGesture(Key.Enter));
            var okCommandBinding = new CommandBinding(OkCommand, OkCommand_Executed);
            this.CommandBindings.Add(okCommandBinding);

            // close command
            CloseCommand.InputGestures.Add(new KeyGesture(Key.Escape));
            var commandBinding = new CommandBinding(CloseCommand, (t, e) => Close());
            this.CommandBindings.Add(commandBinding);

            // events
            //TextBox.LostFocus += (s, e) => Close();
            TextBox.Loaded += TextBox_Loaded;
            TextBox.PreviewKeyDown += TextBox_KeyDown;
        }

        int keyCount;

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 最初の方向入力に限りカーソル位置を固定する
            if (keyCount == 0 && (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Up))
            {
                int pos = TextBox.SelectionStart + TextBox.SelectionLength;
                TextBox.Select(pos, 0);

                keyCount++;
            }
        }

        private void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(NewName);
            TextBox.Select(0, name.Length);
            //TextBox.SelectionStart = name.Length;

            TextBox.Focus();
        }

        private void OkCommand_Executed(object target, ExecutedRoutedEventArgs e)
        {
            if (Rename()) Close();
        }

        private bool Rename()
        {
            string src = File.Path;
            string dst = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(src), NewName);

            if (src == dst) return true;

            // 大文字小文字の変換は正常
            if (string.Compare(src, dst, true) == 0)
            {
                // nop.
            }

            // 重複ファイル名回避
            else if (System.IO.File.Exists(dst) || System.IO.Directory.Exists(dst))
            {
                string dstBase = dst;
                string dir = System.IO.Path.GetDirectoryName(dst);
                string name = System.IO.Path.GetFileNameWithoutExtension(dst);
                string ext = System.IO.Path.GetExtension(dst);
                int count = 1;

                do
                {
                    dst = $"{dir}\\{name} ({++count}){ext}";
                }
                while (System.IO.File.Exists(dst) || System.IO.Directory.Exists(dst));

                // 確認
                var resut = MessageBox.Show($"{System.IO.Path.GetFileName(dstBase)} は既に存在します。\n{System.IO.Path.GetFileName(dst)} に名前を変更しますか？", "名前の変更の確認", MessageBoxButton.OKCancel);
                if (resut != MessageBoxResult.OK)
                {
                    return false;
                }
            }
            
            // 名前変更実行
            try
            {
                if (System.IO.Directory.Exists(src))
                {
                    System.IO.Directory.Move(src, dst);
                }
                else
                {
                    System.IO.File.Move(src, dst);
                }
                File.Path = dst;
                File.OnPropertyChanged("FileName");
            }
            catch (Exception ex)
            {
                MessageBox.Show("名前の変更に失敗しました。\n\n" + ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Rename()) Close();
        }
    }
}
