﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System.Runtime.InteropServices;

using System.Drawing;
using System.Windows.Media.Imaging;

namespace RealtimeSearch
{
    public class FileInfo
    {
        #region SHGetFileInfo
        // SHGetFileInfo関数
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

#if true
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
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

        string path;

        public FileInfo(string path)
        {
            this.path = path;
        }


        private string _TypeName;
        public string TypeName
        {
            get
            {
                if (_TypeName == null) _TypeName = GetTypeName(path);
                return _TypeName;
            }
        }


        private BitmapSource _IconSource;
        public BitmapSource IconSource
        {
            get
            {
                if (_IconSource == null) _IconSource = GetTypeIconSource(path, IconSize.Small);
                return _IconSource;
            }
        }

        private long? _Size;
        public long Size
        {
            get
            {
                if (_Size == null) _Size = GetSize(path);
                return (long)_Size;
            }
        }


        private DateTime? _LastWriteTime;
        public DateTime LastWriteTime
        {
            get
            {
                if (_LastWriteTime == null) _LastWriteTime = GetLastWriteTime(path);
                return (DateTime)_LastWriteTime;
            }
        }




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
            return shfi.szTypeName;
        }


        /// <summary>
        /// ファイルの種類名を取得(Win32版)(USEFILEATTRIBUTES)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetTypeNameExt(string path)
        {
            SHFILEINFO shfi = new SHFILEINFO();
            shfi.szDisplayName = "";
            shfi.szTypeName = "";

            IntPtr hSuccess = SHGetFileInfo(path, 0, ref shfi, (uint)Marshal.SizeOf(shfi), SHGFI_TYPENAME | SHGFI_USEFILEATTRIBUTES);
            return shfi.szTypeName;
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
#if true
                DestroyIcon(shinfo.hIcon);
#endif
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
        public static BitmapSource GetTypeIconSourceExt(string path, IconSize iconSize)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            IntPtr hSuccess = SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | (iconSize == IconSize.Small ? SHGFI_SMALLICON : SHGFI_LARGEICON) | SHGFI_USEFILEATTRIBUTES);
            if (hSuccess != IntPtr.Zero)
            {
                BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(shinfo.hIcon, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
#if true
                DestroyIcon(shinfo.hIcon);
#endif
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
    }
}