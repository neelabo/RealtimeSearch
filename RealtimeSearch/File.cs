using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Input;
using System.Diagnostics;

using System.ComponentModel;
using System.Collections.ObjectModel;

using Microsoft.VisualBasic;

using System.Drawing;
using System.Runtime.InteropServices;

using System.Windows.Media.Imaging;


namespace RealtimeSearch
{
    public class File
    {
        private string path;
        public string Path
        {
            get { return path; }
            set { path = value; NormalizedWord = ToNormalisedWord(FileName); }
        }
        public string DirectoryName
        {
            get
            {
                string dir = System.IO.Path.GetDirectoryName(path);
                return System.IO.Path.GetFileName(dir) + " (" + System.IO.Path.GetDirectoryName(dir) + ")";
            }
        }
        public string FileName { get { return System.IO.Path.GetFileName(path); } }

        public string NormalizedWord;

        /// <summary>
        /// ファイルサイズ
        /// </summary>
        private long size = -1;
        public long Size
        {
            get
            {
                if (size < 0) size = FileInfo.GetSize(path);
                return size;
            }
        }

        private string typeName;
        public string TypeName
        {
            get
            {
                if (typeName == null) typeName = FileInfo.GetTypeName(path);
                return typeName;
            }
        }

        private BitmapSource iconSource;
        public BitmapSource IconSource
        {
            get
            {
                if (iconSource == null) iconSource = FileInfo.GetTypeIconSource(path, FileInfo.IconSize.Small);
                return iconSource;
            }
        }

        private string lastWriteTime;
        public string LastWriteTime
        {
            get
            {
                if (lastWriteTime == null)
                {
                    DateTime dateTime = FileInfo.GetLastWriteTime(path);
                    lastWriteTime = dateTime.ToShortDateString() + " " + dateTime.ToShortTimeString();
                }
                return lastWriteTime;
            }
        }

        public ICommand OpenFile { set; get; }
        public ICommand OpenPlace { set; get; }
        public ICommand CopyFileName { set; get; }

        public File()
        {
            OpenFile = new CommandOpenFileItem();
            OpenPlace = new CommandOpenPlace();
            CopyFileName = new CommandCopyFileName();

            //ToNormalisedWord("ＡＢＣ０１２");
            //ToNormalisedWord("ABCＡＢＣ。｡いろはﾊﾞイロハｲﾛﾊ＃：");
        }

        //
        public static string ToNormalisedWord(string src)
        {
            //  new Regex("[０-９Ａ-Ｚａ-ｚ：－　]+")
            // 
            string s = src;
            s = Strings.StrConv(s, VbStrConv.Wide); // 全角文字にする
            s = Strings.StrConv(s, VbStrConv.Hiragana); // カタカナをひらがなにする
            s = Strings.StrConv(s, VbStrConv.Uppercase); // 大文字にする
            s = s.Replace("　", ""); // 空白を削除する
            //s = Strings.StrConv(s, VbStrConv.Narrow); // 全角文字を半角文字にする

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

    // 
    public class FileList : ObservableCollection<File>
    {
        public FileList()
        {
            // 複数スレッドからコレクション操作できるようにする
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(this, new object());
            //Add(@"E:\Work\同人誌\(同人誌) [vaissu (茶琉)] 露出少女日記6冊目.zip");
        }

        public void Add(string path)
        {
            var file = new File() { Path = path };
            Add(file);
        }
    }


    public static class FileInfo
    {
        #region SHGetFileInfo
        // SHGetFileInfo関数
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        //[DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

#if false
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DestroyIcon(IntPtr hIcon);
#endif

        // SHGetFileInfo関数で使用するフラグ
        private const uint SHGFI_ICON = 0x100; // アイコン・リソースの取得
        private const uint SHGFI_LARGEICON = 0x0; // 大きいアイコン
        private const uint SHGFI_SMALLICON = 0x1; // 小さいアイコン
        private const uint SHGFI_TYPENAME = 0x400;//ファイルの種類

        private const uint SHGFI_USEFILEATTRIBUTES = 0x10; // ?

        // SHGetFileInfo関数で使用する構造体
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };
        #endregion

#if false
        //VB 関数をC#に移植する: Chr()
        private static Char Chr(int i)
        {
            //指定した値を Unicode 文字に変換します。
            return Convert.ToChar(i);
        }
#endif

        // ファイルの種類名を取得
        public static string GetTypeName(string path)
        {
            SHFILEINFO shfi = new SHFILEINFO();
            //shfi.szDisplayName = new string(Chr(0), 260);
            //shfi.szTypeName = new string(Chr(0), 80);

            shfi.szDisplayName = "";
            shfi.szTypeName = "";
            
            //IntPtr hSuccess = SHGetFileInfo(path, 0, ref shfi, (uint)Marshal.SizeOf(shfi), SHGFI_TYPENAME | SHGFI_USEFILEATTRIBUTES);
            IntPtr hSuccess = SHGetFileInfo(path, 0, ref shfi, (uint)Marshal.SizeOf(shfi), SHGFI_TYPENAME | SHGFI_USEFILEATTRIBUTES);
            return shfi.szTypeName;

            //return "HOGE";
        }

        public enum IconSize
        {
            Small,
            Normal,
        };

        // アプリケーション・アイコンを取得
        public static Icon GetTypeIcon(string path, IconSize iconSize)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            IntPtr hSuccess = SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | (iconSize == IconSize.Small ? SHGFI_SMALLICON : SHGFI_LARGEICON) | SHGFI_USEFILEATTRIBUTES);
            return Icon.FromHandle(shinfo.hIcon);
        }


        // アプリケーション・アイコンを取得
        public static BitmapSource GetTypeIconSource(string path, IconSize iconSize)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            IntPtr hSuccess = SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | (iconSize == IconSize.Small ? SHGFI_SMALLICON : SHGFI_LARGEICON) | SHGFI_USEFILEATTRIBUTES);
            if (hSuccess != IntPtr.Zero)
            {
                BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(shinfo.hIcon, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
#if false
                DestroyIcon(shinfo.hIcon);
#endif
                return bitmapSource;
            }
            else
            {
                return null;
            }
        }


        // アプリケーション・アイコンを取得
        public static BitmapSource GetTypeIconSourceEx(string path, IconSize iconSize)
        {
            Icon appIcon = System.Drawing.Icon.ExtractAssociatedIcon(path);
            //return appIcon; //pictureBox1.Image = appIcon.ToBitmap();
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(appIcon.ToBitmap().GetHicon(), System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            //return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(appIcon.ToBitmap().GetHicon(), System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromHeight(16));
        }


        // アプリケーション・アイコンを取得
        public static Icon GetIcon(string path)
        {
            Icon appIcon = System.Drawing.Icon.ExtractAssociatedIcon(path);
            return appIcon; //pictureBox1.Image = appIcon.ToBitmap();
        }


        // ファイルサイズ取得
        public static long GetSize(string path)
        {
            var fileInfo = new System.IO.FileInfo(path);
            return fileInfo.Length;
        }

        // ファイル更新日取得
        public static DateTime GetLastWriteTime(string path)
        {
            var fileInfo = new System.IO.FileInfo(path);
            return fileInfo.LastWriteTime;
        }
    }



}
