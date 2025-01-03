﻿// from http://grabacr.net/archives/1585
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace NeeLaboratory.RealtimeSearch.Windows
{
    // TODO: AeroSnap保存ON/OFFフラグ。WindowPlacementOptionフラグ？
    public static class WindowPlacementTools
    {
        #region NativeApi

        internal static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern bool SetWindowPlacement(nint hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

            [DllImport("user32.dll")]
            public static extern bool GetWindowPlacement(nint hWnd, out WINDOWPLACEMENT lpwndpl);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool GetWindowRect(nint hwnd, out RECT lpRect);


            [Serializable]
            [StructLayout(LayoutKind.Sequential)]
            public struct WINDOWPLACEMENT
            {
                public int length;
                public int flags;
                public SW showCmd;
                public POINT minPosition;
                public POINT maxPosition;
                public RECT normalPosition;

                public int Length
                {
                    get => length;
                    set => length = value;
                }

                public int Flags
                {
                    get => flags;
                    set => flags = value;
                }

                public SW ShowCmd
                {
                    get => showCmd;
                    set => showCmd = value;
                }

                public POINT MinPosition
                {
                    get => minPosition;
                    set => minPosition = value;
                }

                public POINT MaxPosition
                {
                    get => maxPosition;
                    set => maxPosition = value;
                }

                public RECT NormalPosition
                {
                    get => normalPosition;
                    set => normalPosition = value;
                }

                public bool IsValid() => length == Marshal.SizeOf(typeof(WINDOWPLACEMENT));

                public override string ToString()
                {
                    return $"{ShowCmd},{MinPosition.X},{MinPosition.Y},{MaxPosition.X},{MaxPosition.Y},{NormalPosition.Left},{NormalPosition.Top},{NormalPosition.Right},{NormalPosition.Bottom}";
                }
            }


            [Serializable]
            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int X;
                public int Y;

                public POINT(int x, int y)
                {
                    X = x;
                    Y = y;
                }

                public override string ToString()
                {
                    return $"{X},{Y}";
                }

                public static POINT Parse(string s)
                {
                    if (string.IsNullOrWhiteSpace(s)) throw new InvalidCastException();

                    var tokens = s.Split(',');
                    if (tokens.Length != 2) throw new InvalidCastException();

                    return new POINT(int.Parse(tokens[0]), int.Parse(tokens[1]));
                }
            }


            [Serializable]
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;

                public RECT(int left, int top, int right, int bottom)
                {
                    Left = left;
                    Top = top;
                    Right = right;
                    Bottom = bottom;
                }

                public int Width
                {
                    get => Right - Left;
                    set => Right = Left + value;
                }

                public int Height
                {
                    get => Bottom - Top;
                    set => Bottom = Top + value;
                }

                public override string ToString()
                {
                    return $"{Left},{Top},{Right},{Bottom}";
                }

                public static RECT Parse(string s)
                {
                    if (string.IsNullOrWhiteSpace(s)) throw new InvalidCastException();

                    var tokens = s.Split(',');
                    if (tokens.Length != 4) throw new InvalidCastException();

                    return new RECT(int.Parse(tokens[0]), int.Parse(tokens[1]), int.Parse(tokens[2]), int.Parse(tokens[3]));
                }
            }

            public enum SW
            {
                HIDE = 0,
                SHOWNORMAL = 1,
                SHOWMINIMIZED = 2,
                SHOWMAXIMIZED = 3,
                SHOWNOACTIVATE = 4,
                SHOW = 5,
                MINIMIZE = 6,
                SHOWMINNOACTIVE = 7,
                SHOWNA = 8,
                RESTORE = 9,
                SHOWDEFAULT = 10,
            }


            #region for AeroSnap

            private const int CCHDEVICENAME = 32;

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public struct MONITORINFOEX
            {
                public int cbSize; // initialize this field using: Marshal.SizeOf(typeof(MONITORINFOEX));
                public RECT rcMonitor;
                public RECT rcWork;
                public uint dwFlags;

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
                public string szDeviceName;
            }

            [DllImport("user32.dll")]
            public static extern nint MonitorFromRect([In] ref RECT lprc, uint dwFlags);

            public const uint MONITOR_MONITOR_DEFAULTTONULL = 0x00000000;
            public const uint MONITOR_MONITOR_DEFAULTTOPRIMARY = 0x00000001;
            public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFOEX lpmi);

            [StructLayout(LayoutKind.Sequential)]
            public struct APPBARDATA
            {
                public int cbSize; // initialize this field using: Marshal.SizeOf(typeof(APPBARDATA));
                public nint hWnd;
                public uint uCallbackMessage;
                public uint uEdge;
                public RECT rc;
                public int lParam;
            }

            [DllImport("shell32.dll")]
            public static extern nint SHAppBarMessage(uint dwMessage, [In] ref APPBARDATA pData);

            public const uint ABM_NEW = 0;
            public const uint ABM_REMOVE = 1;
            public const uint ABM_QUERYPOS = 2;
            public const uint ABM_SETPOS = 3;
            public const uint ABM_GETSTATE = 4;
            public const uint ABM_GETTASKBARPOS = 5;
            public const uint ABM_ACTIVATE = 6;
            public const uint ABM_GETAUTOHIDEBAR = 7;
            public const uint ABM_SETAUTOHIDEBAR = 8;
            public const uint ABM_WINDOWPOSCHANGED = 9;
            public const uint ABM_SETSTATE = 10;

            public const uint ABE_LEFT = 0;
            public const uint ABE_TOP = 1;
            public const uint ABE_RIGHT = 2;
            public const uint ABE_BOTTOM = 3;

            #endregion for AeroSnap
        }

        #endregion Native


        public static WindowPlacement StoreWindowPlacement(Window window, bool withAeroSnap)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == nint.Zero) throw new InvalidOperationException();

            ////if (!(window is IDpiScaleProvider dpiProvider)) throw new ArgumentException($"need window has IDpiProvider.");

            NativeMethods.GetWindowPlacement(hwnd, out NativeMethods.WINDOWPLACEMENT raw);
            ////Debug.WriteLine($"WindowPlacement.Store: Native.WindowPlacement: {raw}");

            if (withAeroSnap)
            {
                if (raw.ShowCmd == NativeMethods.SW.SHOWNORMAL)
                {
                    try
                    {
                        // AeroSnapの座標保存
                        // NOTE: スナップ状態の復元方法が不明なため、現在のウィンドウサイズを通常ウィンドウサイズとして上書きする。
                        raw.NormalPosition = GetAeroPlacement(hwnd);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }

            // DPI補正
            var dpi = VisualTreeHelper.GetDpi(window);
            raw.normalPosition.Width = (int)(raw.normalPosition.Width / dpi.DpiScaleX);
            raw.normalPosition.Height = (int)(raw.normalPosition.Height / dpi.DpiScaleY);
            ////Debug.WriteLine($"WindowPlacement.Restore: WIDTH: {raw.normalPosition.Width}, DPI: {dpi.DpiScaleX}");

            return ConvertToWindowPlacement(raw);
        }


        // from http://oldworldgarage.web.fc2.com/programing/tip0006_RestoreWindow.html
        private static NativeMethods.RECT GetAeroPlacement(nint hwnd)
        {
            NativeMethods.GetWindowRect(hwnd, out NativeMethods.RECT rect);

            // ウィンドウのあるモニターハンドルを取得
            nint hMonitor = NativeMethods.MonitorFromRect(ref rect, NativeMethods.MONITOR_DEFAULTTONEAREST);

            // モニター情報取得
            //var monitorInfo = new NativeMethods.MONITORINFOEX();
            //monitorInfo.cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFOEX));
            //monitorInfo.szDeviceName = "";
            //NativeMethods.GetMonitorInfo(hMonitor, ref monitorInfo);

            // タスクバーのあるモニターハンドルを取得
            var appBarData = new NativeMethods.APPBARDATA
            {
                cbSize = Marshal.SizeOf(typeof(NativeMethods.APPBARDATA)),
                hWnd = nint.Zero
            };
            NativeMethods.SHAppBarMessage(NativeMethods.ABM_GETTASKBARPOS, ref appBarData);
            nint hMonitorWithTaskBar = NativeMethods.MonitorFromRect(ref appBarData.rc, NativeMethods.MONITOR_DEFAULTTONEAREST);

            // ウィンドウとタスクバーが同じモニターにある？
            if (hMonitor == hMonitorWithTaskBar)
            {
                // 常に表示？
                if (NativeMethods.SHAppBarMessage(NativeMethods.ABM_GETAUTOHIDEBAR, ref appBarData) == nint.Zero)
                {
                    // 座標補正
                    NativeMethods.SHAppBarMessage(NativeMethods.ABM_GETTASKBARPOS, ref appBarData);
                    switch (appBarData.uEdge)
                    {
                        case NativeMethods.ABE_TOP:
                            rect.Top -= appBarData.rc.Bottom - appBarData.rc.Top;
                            rect.Bottom -= appBarData.rc.Bottom - appBarData.rc.Top;
                            break;
                        case NativeMethods.ABE_LEFT:
                            rect.Left -= appBarData.rc.Right - appBarData.rc.Left;
                            rect.Right -= appBarData.rc.Right - appBarData.rc.Left;
                            break;
                    }
                }
            }

            return rect;
        }


        public static void RestoreWindowPlacement(Window window, WindowPlacement placement)
        {
            if (placement == null || !placement.IsValid()) return;

            ///if (!(window is IDpiScaleProvider dpiProvider)) throw new ArgumentException($"need window has IDpiProvider.");

            var hwnd = new WindowInteropHelper(window).Handle;
            var raw = ConvertToNativeWindowPlacement(placement);

            // DPI補正
            var dpi = VisualTreeHelper.GetDpi(window);
            raw.normalPosition.Width = (int)(raw.normalPosition.Width * dpi.DpiScaleX + 0.5);
            raw.normalPosition.Height = (int)(raw.normalPosition.Height * dpi.DpiScaleY + 0.5);
            ////Debug.WriteLine($"WindowPlacement.Restore: WIDTH: {raw.normalPosition.Width}, DPI: {dpi.DpiScaleX}");

            NativeMethods.SetWindowPlacement(hwnd, ref raw);
        }


        private static WindowPlacement ConvertToWindowPlacement(NativeMethods.WINDOWPLACEMENT raw)
        {
            var memento = new WindowPlacement(
                ConvertToWindowState(raw.ShowCmd),
                raw.NormalPosition.Left,
                raw.NormalPosition.Top,
                raw.NormalPosition.Width,
                raw.NormalPosition.Height);
            return memento;
        }

        private static NativeMethods.WINDOWPLACEMENT ConvertToNativeWindowPlacement(WindowPlacement placement)
        {
            var raw = new NativeMethods.WINDOWPLACEMENT
            {
                Length = Marshal.SizeOf(typeof(NativeMethods.WINDOWPLACEMENT)),
                Flags = 0,
                ShowCmd = ConvertToNativeShowCmd(placement.WindowState),
                MinPosition = new NativeMethods.POINT(-1, -1),
                MaxPosition = new NativeMethods.POINT(-1, -1),
                NormalPosition = new NativeMethods.RECT(placement.Left, placement.Top, placement.Right, placement.Bottom)
            };
            return raw;
        }

        private static WindowState ConvertToWindowState(NativeMethods.SW showCmd)
        {
            return showCmd switch
            {
                NativeMethods.SW.SHOWMINIMIZED => WindowState.Minimized,
                NativeMethods.SW.SHOWMAXIMIZED => WindowState.Maximized,
                _ => WindowState.Normal,
            };
        }

        private static NativeMethods.SW ConvertToNativeShowCmd(WindowState windowState)
        {
            return windowState switch
            {
                WindowState.Minimized => NativeMethods.SW.SHOWMINIMIZED,
                WindowState.Maximized => NativeMethods.SW.SHOWMAXIMIZED,
                _ => NativeMethods.SW.SHOWNORMAL,
            };
        }
    }

}
