// Copyright (c) 2015-2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace RealtimeSearch
{
    public class FileInfo
    {
        #region SHGetFileInfo
        // SHGetFileInfo関数
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        // DestroyIcon関数
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DestroyIcon(IntPtr hIcon);

        // SHObjectProperties関数
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern bool SHObjectProperties(IntPtr hwnd, uint shopObjectType, [MarshalAs(UnmanagedType.LPWStr)] string pszObjectName, [MarshalAs(UnmanagedType.LPWStr)] string pszPropertyPage);

        private const uint SHOP_PRINTERNAME = 0x1;
        private const uint SHOP_FILEPATH = 0x2;
        private const uint SHOP_VOLUMEGUID = 0x4;

        // SHGetFileInfo関数で使用するフラグ
        private const uint SHGFI_ICON = 0x100; // アイコン・リソースの取得
        private const uint SHGFI_LARGEICON = 0x0; // 大きいアイコン
        private const uint SHGFI_SMALLICON = 0x1; // 小さいアイコン
        private const uint SHGFI_TYPENAME = 0x400; //ファイルの種類

        private const uint SHGFI_USEFILEATTRIBUTES = 0x10; // fileAttributeを使用する

        //
        private const uint FILE_ATTRIBUTE_READONLY = 0x0001;
        private const uint FILE_ATTRIBUTE_HIDDEN = 0x0002;
        private const uint FILE_ATTRIBUTE_SYSTEM = 0x0004;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x0010;
        private const uint FILE_ATTRIBUTE_ARCHIVE = 0x0020;
        private const uint FILE_ATTRIBUTE_ENCRYPTED = 0x0040;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x0080;
        private const uint FILE_ATTRIBUTE_TEMPORARY = 0x0100;
        private const uint FILE_ATTRIBUTE_SPARSE_FILE = 0x0200;
        private const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x0400;
        private const uint FILE_ATTRIBUTE_COMPRESSED = 0x0800;
        private const uint FILE_ATTRIBUTE_OFFLINE = 0x1000;

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

        private string _Path;
        private bool _IsDirectory;


        // constructor
        public FileInfo(string path)
        {
            _Path = path;
            _IsDirectory = System.IO.Directory.Exists(path);
        }


        private string _TypeName;
        public string TypeName
        {
            get
            {
                if (_TypeName == null) _TypeName = CreateTypeName(_Path, _IsDirectory);
                return _TypeName;
            }
        }

        //
        private BitmapSource _IconSource;
        public BitmapSource IconSource
        {
            get
            {
                if (_IconSource == null) _IconSource = CreateIcon(_Path, _IsDirectory);
                return _IconSource;
            }
        }


        private long? _Size;
        public long Size
        {
            get
            {
                if (_Size == null) _Size = GetSize(_Path);
                return (long)_Size;
            }
        }


        private DateTime? _LastWriteTime;
        public DateTime LastWriteTime
        {
            get
            {
                if (_LastWriteTime == null) _LastWriteTime = GetLastWriteTime(_Path);
                return (DateTime)_LastWriteTime;
            }
        }



        #region アイコンリソース取得 (簡易版)

        static Dictionary<string, string> _TypeNameDictionary = new Dictionary<string, string>();
        static Dictionary<string, BitmapSource> _IconDictionary = new Dictionary<string, BitmapSource>();

        static string _FolderTypeName;
        static BitmapSource _FolderIcon;
        static string _DefaultTypeName;
        static BitmapSource _DefaultIcon;

        /// <summary>
        /// ファイルの標準アイコンを準備
        /// </summary>
        public static void InitializeDefaultResource()
        {
            const string path = "__dummy_file__";
            _FolderTypeName = GetTypeNameWithAttribute(path, FILE_ATTRIBUTE_DIRECTORY);
            _FolderIcon = GetTypeIconSourceWithAttribute(path, IconSize.Small, FILE_ATTRIBUTE_DIRECTORY);
            _DefaultTypeName = GetTypeNameWithAttribute(path, 0);
            _DefaultIcon = GetTypeIconSourceWithAttribute(path, IconSize.Small, 0);
        }


        /// <summary>
        /// ファイルタイプ名取得 (軽量版)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isDirectory"></param>
        /// <returns></returns>
        private static string CreateTypeName(string path, bool isDirectory)
        {
            string typeName = null;

            if (isDirectory)
            {
                typeName = _FolderTypeName;
            }
            else
            {
                string ext = System.IO.Path.GetExtension(path).ToLower();
                if (!string.IsNullOrEmpty(ext))
                {
                    if (!_TypeNameDictionary.TryGetValue(ext, out typeName))
                    {
                        typeName = GetTypeNameWithAttribute(ext, 0);
                        if (typeName != null)
                        {
                            _TypeNameDictionary.Add(ext, typeName);
                        }
                    }
                }
            }

            return typeName ?? _DefaultTypeName;
        }


        /// <summary>
        /// アイコン取得  (軽量版)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isDirectory"></param>
        /// <returns></returns>
        private static BitmapSource CreateIcon(string path, bool isDirectory)
        {
            BitmapSource icon = null;

            if (isDirectory)
            {
                icon = _FolderIcon;
            }
            else
            {
                string ext = System.IO.Path.GetExtension(path).ToLower();
                if (!string.IsNullOrEmpty(ext))
                {
                    if (!_IconDictionary.TryGetValue(ext, out icon))
                    {
                        icon = GetTypeIconSourceWithAttribute(ext, IconSize.Small, 0);
                        if (icon != null)
                        {
                            _IconDictionary.Add(ext, icon);
                        }
                    }
                }
            }
            return icon ?? _DefaultIcon;
        }

        #endregion



        /// <summary>
        /// ファイルの種類名を取得(Win32版)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetTypeName(string path)
        {
            SHFILEINFO shfi = new SHFILEINFO();
            shfi.szDisplayName = "";
            shfi.szTypeName = "";

            IntPtr hSuccess = SHGetFileInfo(path, 0, ref shfi, (uint)Marshal.SizeOf(shfi), SHGFI_TYPENAME);
            if (!string.IsNullOrEmpty(shfi.szTypeName))
            {
                return shfi.szTypeName;
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// ファイルの種類名を取得(Win32版)(USEFILEATTRIBUTES)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetTypeNameWithAttribute(string path, uint attribute)
        {
            SHFILEINFO shfi = new SHFILEINFO();
            shfi.szDisplayName = "";
            shfi.szTypeName = "";

            IntPtr hSuccess = SHGetFileInfo(path, attribute, ref shfi, (uint)Marshal.SizeOf(shfi), SHGFI_TYPENAME | SHGFI_USEFILEATTRIBUTES);
            if (!string.IsNullOrEmpty(shfi.szTypeName))
            {
                return shfi.szTypeName;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public enum IconSize
        {
            Small,
            Normal,
        };


        /// <summary>
        /// アプリケーション・アイコンを取得(Win32版)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="iconSize"></param>
        /// <returns></returns>
        public static BitmapSource GetTypeIconSource(string path, IconSize iconSize)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            IntPtr hSuccess = SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | (iconSize == IconSize.Small ? SHGFI_SMALLICON : SHGFI_LARGEICON));
            if (hSuccess != IntPtr.Zero)
            {
                BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(shinfo.hIcon, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                DestroyIcon(shinfo.hIcon);
                return bitmapSource;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        // アプリケーション・アイコンを取得(Win32版)(USEFILEATTRIBUTES)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="iconSize"></param>
        /// <returns></returns>
        public static BitmapSource GetTypeIconSourceWithAttribute(string path, IconSize iconSize, uint attribute)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            IntPtr hSuccess = SHGetFileInfo(path, attribute, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | (iconSize == IconSize.Small ? SHGFI_SMALLICON : SHGFI_LARGEICON) | SHGFI_USEFILEATTRIBUTES);
            if (hSuccess != IntPtr.Zero)
            {
                BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(shinfo.hIcon, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                DestroyIcon(shinfo.hIcon);
                return bitmapSource;
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// ファイルサイズ取得
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static long GetSize(string path)
        {
            var fileInfo = new System.IO.FileInfo(path);
            if ((fileInfo.Attributes & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory)
            {
                return -1;
            }
            else
            {
                return fileInfo.Length;
            }
        }

        /// <summary>
        /// ファイル更新日取得
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static DateTime GetLastWriteTime(string path)
        {
            var fileInfo = new System.IO.FileInfo(path);
            return fileInfo.LastWriteTime;
        }


        /// <summary>
        /// プロパティウィンドウを開く
        /// </summary>
        /// <param name="path"></param>
        public static void OpenProperty(System.Windows.Window window, string path)
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;

            if (!SHObjectProperties(handle, SHOP_FILEPATH, path, string.Empty))
            {
                throw new ApplicationException("プロパティウィンドウを開けませんでした");
            }
        }
    }
}