using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace NeeLaboratory.RealtimeSearch
{
    /// <summary>
    /// ファイルシステム静的メソッド
    /// </summary>
    public class ShellFileResource
    {
        #region NativeMethods

        internal static class NativeMethods
        {
            // SHGetFileInfo関数
            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

            // DestroyIcon関数
            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DestroyIcon(IntPtr hIcon);

            // SHObjectProperties関数
            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            public static extern bool SHObjectProperties(IntPtr hwnd, uint shopObjectType, [MarshalAs(UnmanagedType.LPWStr)] string pszObjectName, [MarshalAs(UnmanagedType.LPWStr)] string pszPropertyPage);

            public const uint SHOP_PRINTERNAME = 0x1;
            public const uint SHOP_FILEPATH = 0x2;
            public const uint SHOP_VOLUMEGUID = 0x4;

            // SHGetFileInfo関数で使用するフラグ
            public const uint SHGFI_ICON = 0x100; // アイコン・リソースの取得
            public const uint SHGFI_LARGEICON = 0x0; // 大きいアイコン
            public const uint SHGFI_SMALLICON = 0x1; // 小さいアイコン
            public const uint SHGFI_TYPENAME = 0x400; //ファイルの種類

            public const uint SHGFI_USEFILEATTRIBUTES = 0x10; // fileAttributeを使用する

            //
            public const uint FILE_ATTRIBUTE_READONLY = 0x0001;
            public const uint FILE_ATTRIBUTE_HIDDEN = 0x0002;
            public const uint FILE_ATTRIBUTE_SYSTEM = 0x0004;
            public const uint FILE_ATTRIBUTE_DIRECTORY = 0x0010;
            public const uint FILE_ATTRIBUTE_ARCHIVE = 0x0020;
            public const uint FILE_ATTRIBUTE_ENCRYPTED = 0x0040;
            public const uint FILE_ATTRIBUTE_NORMAL = 0x0080;
            public const uint FILE_ATTRIBUTE_TEMPORARY = 0x0100;
            public const uint FILE_ATTRIBUTE_SPARSE_FILE = 0x0200;
            public const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x0400;
            public const uint FILE_ATTRIBUTE_COMPRESSED = 0x0800;
            public const uint FILE_ATTRIBUTE_OFFLINE = 0x1000;

            // SHGetFileInfo関数で使用する構造体
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public struct SHFILEINFO
            {
                public IntPtr hIcon;
                public int iIcon;
                public uint dwAttributes;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                public string szDisplayName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
                public string szTypeName;
            };
        }

        #endregion

        /// <summary>
        /// アイコンサイズ
        /// </summary>
        public enum IconSize
        {
            Small,
            Normal,
        };


        const string _dummyFilePath = "__dummy_file__";

        private static readonly Dictionary<string, string> _typeNameDictionary = new();
        private static readonly Dictionary<string, BitmapSource> _iconDictionary = new();
        private static readonly Lazy<string> _folderTypeName = new(() => GetTypeNameWithAttribute(_dummyFilePath, NativeMethods.FILE_ATTRIBUTE_DIRECTORY));
        private static readonly Lazy<BitmapSource> _folderIcon = new(() => GetTypeIconSourceWithAttribute(_dummyFilePath, IconSize.Small, NativeMethods.FILE_ATTRIBUTE_DIRECTORY) ?? new BitmapImage());
        private static readonly Lazy<string> _defaultTypeName = new(() => GetTypeNameWithAttribute(_dummyFilePath, 0));
        private static readonly Lazy<BitmapSource> _defaultIcon = new(() => GetTypeIconSourceWithAttribute(_dummyFilePath, IconSize.Small, 0) ?? new BitmapImage());


        /// <summary>
        /// ファイルタイプ名取得 (軽量版)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isDirectory"></param>
        /// <returns></returns>
        public static string CreateTypeName(string path, bool isDirectory)
        {
            string? typeName = null;

            if (isDirectory)
            {
                typeName = _folderTypeName.Value;
            }
            else
            {
                string ext = System.IO.Path.GetExtension(path).ToLower();
                if (!string.IsNullOrEmpty(ext))
                {
                    if (!_typeNameDictionary.TryGetValue(ext, out typeName))
                    {
                        typeName = GetTypeNameWithAttribute(ext, 0);
                        if (!string.IsNullOrEmpty(typeName))
                        {
                            _typeNameDictionary.Add(ext, typeName);
                        }
                    }
                }
            }

            return typeName ?? _defaultTypeName.Value;
        }


        /// <summary>
        /// アイコン取得  (軽量版)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isDirectory"></param>
        /// <returns></returns>
        public static BitmapSource CreateIcon(string path, bool isDirectory)
        {
            BitmapSource? icon = null;

            if (isDirectory)
            {
                icon = _folderIcon.Value;
            }
            else
            {
                string ext = System.IO.Path.GetExtension(path).ToLower();
                if (!string.IsNullOrEmpty(ext))
                {
                    if (!_iconDictionary.TryGetValue(ext, out icon))
                    {
                        icon = GetTypeIconSourceWithAttribute(ext, IconSize.Small, 0);
                        if (icon != null)
                        {
                            _iconDictionary.Add(ext, icon);
                        }
                    }
                }
            }
            return icon ?? _defaultIcon.Value;
        }


        /// <summary>
        /// ファイルの種類名を取得(Win32版)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetTypeName(string path)
        {
            var shfi = new NativeMethods.SHFILEINFO
            {
                szDisplayName = "",
                szTypeName = ""
            };

            IntPtr hSuccess = NativeMethods.SHGetFileInfo(path, 0, ref shfi, (uint)Marshal.SizeOf(shfi), NativeMethods.SHGFI_TYPENAME);
            if (hSuccess != IntPtr.Zero && !string.IsNullOrEmpty(shfi.szTypeName))
            {
                return shfi.szTypeName;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// ファイルの種類名を取得(Win32版)(USEFILEATTRIBUTES)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetTypeNameWithAttribute(string path, uint attribute)
        {
            var shfi = new NativeMethods.SHFILEINFO
            {
                szDisplayName = "",
                szTypeName = ""
            };

            IntPtr hSuccess = NativeMethods.SHGetFileInfo(path, attribute, ref shfi, (uint)Marshal.SizeOf(shfi), NativeMethods.SHGFI_TYPENAME | NativeMethods.SHGFI_USEFILEATTRIBUTES);
           if (hSuccess != IntPtr.Zero && !string.IsNullOrEmpty(shfi.szTypeName))
            {
                return shfi.szTypeName;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// アプリケーション・アイコンを取得(Win32版)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="iconSize"></param>
        /// <returns></returns>
        public static BitmapSource? GetTypeIconSource(string path, IconSize iconSize)
        {
            var shinfo = new NativeMethods.SHFILEINFO();
            IntPtr hSuccess = NativeMethods.SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), NativeMethods.SHGFI_ICON | (iconSize == IconSize.Small ? NativeMethods.SHGFI_SMALLICON : NativeMethods.SHGFI_LARGEICON));
            if (hSuccess != IntPtr.Zero)
            {
                BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(shinfo.hIcon, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                NativeMethods.DestroyIcon(shinfo.hIcon);
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
        public static BitmapSource? GetTypeIconSourceWithAttribute(string path, IconSize iconSize, uint attribute)
        {
            var shinfo = new NativeMethods.SHFILEINFO();
            IntPtr hSuccess = NativeMethods.SHGetFileInfo(path, attribute, ref shinfo, (uint)Marshal.SizeOf(shinfo), NativeMethods.SHGFI_ICON | (iconSize == IconSize.Small ? NativeMethods.SHGFI_SMALLICON : NativeMethods.SHGFI_LARGEICON) | NativeMethods.SHGFI_USEFILEATTRIBUTES);
            if (hSuccess != IntPtr.Zero)
            {
                BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(shinfo.hIcon, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                NativeMethods.DestroyIcon(shinfo.hIcon);
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

            if (!NativeMethods.SHObjectProperties(handle, NativeMethods.SHOP_FILEPATH, path, string.Empty))
            {
                throw new ApplicationException("Cannot open property window");
            }
        }
    }
}
