using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Input;
using System.Diagnostics;

using System.ComponentModel;
using System.Collections.ObjectModel;
//using Microsoft.VisualBasic;

using System.IO;


namespace RealtimeSearch
{
    // 数が尋常でないので、軽量にすべき
    public class File
    {
        private string path;
        public string Path
        {
            get { return path; }
            set { path = value; NormalizedWord = ToNormalisedWord(FileName); FileInfo = new FileInfo(path); }
        }
        //private string _DirectoryName;
        public string DirectoryName
        {
            get
            {
                //if (_DirectoryName == null)
                //{
                    string dir = System.IO.Path.GetDirectoryName(path);
                    string parentDir = System.IO.Path.GetDirectoryName(dir);
                    return (parentDir == null) ? dir : System.IO.Path.GetFileName(dir) + " (" + parentDir + ")";
                //}
                //return _DirectoryName;
            }
        }
        public string FileName { get { return System.IO.Path.GetFileName(path); } }

        public string NormalizedWord;

        //public bool IsDirectory;

        // ファイル情報
        public FileInfo FileInfo { get; private set; }

        public static ICommand OpenFile { set; get; }
        public static ICommand OpenPlace { set; get; }
        public static ICommand CopyFileName { set; get; }

        public File()
        {
            // これヤバイ。RootedCommand化すべき
            OpenFile = new CommandOpenFileItem();
            OpenPlace = new CommandOpenPlace();
            CopyFileName = new CommandCopyFileName();

            //ToNormalisedWord("ＡＢＣ０１２巻");
            //ToNormalisedWord("ABCＡＢＣabc。｡　い ろはﾊﾞイロハｲﾛﾊ＃：");
        }

        //
        public static string ToNormalisedWord(string src)
        {
            string s = src.Normalize(NormalizationForm.FormKC); // 正規化
            s = s.Replace(" ", ""); // 空白を削除する

            s = s.ToUpper(); // アルファベットを大文字にする
            s = Microsoft.VisualBasic.Strings.StrConv(s, Microsoft.VisualBasic.VbStrConv.Katakana); // ひらがなをカタカナにする
            s = s.Replace("ー", "-"); // 長音をハイフンにする 

            return s;
        }
    }

    //
    public class CommandOpenFileItem : ICommand
    {
        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Process.Start(((File)parameter).Path);
        }
    }

    //
    public class CommandOpenPlace : ICommand
    {
        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Process.Start("explorer.exe", "/select,\"" + ((File)parameter).Path + "\"");
        }
    }

    //
    public class CommandCopyFileName : ICommand
    {
        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            string text = System.IO.Path.GetFileNameWithoutExtension(((File)parameter).Path);
            //System.Windows.Clipboard.SetText(text, System.Windows.TextDataFormat.Text);
            System.Windows.Clipboard.SetDataObject(text);
        }
    }


}
