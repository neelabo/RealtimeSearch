// from http://grabacr.net/archives/1585
using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Interop;

namespace NeeLaboratory.RealtimeSearch
{
    public static class WindowPlacement
    {
        #region Native structs

        [Serializable, DataContract]
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            private int _length;
            private int _flags;
            private SHOWCOMMAND _showCmd;
            private POINT _minPosition;
            private POINT _maxPosition;
            private RECT _normalPosition;

            [DataMember]
            public int Length
            {
                get => _length;
                set => _length = value;
            }

            [DataMember]
            public int Flags
            {
                get => _flags;
                set => _flags = value;
            }

            [DataMember]
            public SHOWCOMMAND ShowCmd
            {
                get => _showCmd;
                set => _showCmd = value;
            }

            [DataMember]
            public POINT MinPosition
            {
                get => _minPosition;
                set => _minPosition = value;
            }

            [DataMember]
            public POINT MaxPosition
            {
                get => _maxPosition;
                set => _maxPosition = value;
            }

            [DataMember]
            public RECT NormalPosition
            {
                get => _normalPosition;
                set => _normalPosition = value;
            }

            // 有効判定
            public bool HasValue => _length > 0;
        }

        [Serializable, DataContract]
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            private int _x;
            private int _y;

            public POINT(int x, int y)
            {
                _x = x;
                _y = y;
            }

            [DataMember]
            public int X
            {
                get => _x;
                set => _x = value;
            }

            [DataMember]
            public int Y
            {
                get => _y;
                set => _y = value;
            }
        }

        [Serializable, DataContract]
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            private int _left;
            private int _top;
            private int _right;
            private int _bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                _left = left;
                _top = top;
                _right = right;
                _bottom = bottom;
            }

            [DataMember]
            public int Left
            {
                get => _left;
                set => _left = value;
            }

            [DataMember]
            public int Top
            {
                get => _top;
                set => _top = value;
            }

            [DataMember]
            public int Right
            {
                get => _right;
                set => _right = value;
            }

            [DataMember]
            public int Bottom
            {
                get => _bottom;
                set => _bottom = value;
            }
        }

        public enum SHOWCOMMAND
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

        #endregion

        internal static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

            [DllImport("user32.dll")]
            public static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);
        }


        public static void SetPlacement(Window window, WINDOWPLACEMENT placement)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            placement.Length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
            placement.Flags = 0;
            placement.ShowCmd = (placement.ShowCmd == SHOWCOMMAND.SHOWMINIMIZED) ? SHOWCOMMAND.SHOWNORMAL : placement.ShowCmd;

            NativeMethods.SetWindowPlacement(hwnd, ref placement);
        }

        public static WINDOWPLACEMENT GetPlacement(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) throw new InvalidOperationException();

            NativeMethods.GetWindowPlacement(hwnd, out WINDOWPLACEMENT placement);

            return placement;
        }
    }


}
