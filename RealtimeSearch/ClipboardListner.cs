// Copyright (c) 2015 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace RealtimeSearch
{
    public class ClipboardListner : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private extern static void AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private extern static void RemoveClipboardFormatListener(IntPtr hwnd);

        const int WM_CLOSE = 0x0010;
        const int WM_CLIPBOARDUPDATE = 0x031D;

        Window _Window;
        IntPtr _Handle;

        public event EventHandler ClipboardUpdate;


        private void NotifyClipboardUpdate()
        {
            if (ClipboardUpdate != null)
            {
                if (_Window.IsActive) return;

                _Window.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ClipboardUpdate(this, EventArgs.Empty);
                }));
            }
        }


        public ClipboardListner(Window window)
        {
            Open(window);
        }


        public void Open(Window window)
        {
            if (_Handle != IntPtr.Zero) throw new ApplicationException("ClipboardListner is already opened.");

            _Window = window;
            _Handle = new WindowInteropHelper(window).Handle;

            AddClipboardFormatListener(_Handle);

            HwndSource source = HwndSource.FromHwnd(_Handle);
            source.AddHook(new HwndSourceHook(WndProc));
        }

        
        public void Close()
        {
            if (_Handle != IntPtr.Zero)
            {
                RemoveClipboardFormatListener(_Handle);
                _Handle = IntPtr.Zero;
            }
        }

        
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                NotifyClipboardUpdate();
            }

            return IntPtr.Zero;
        }

        
        public void Dispose()
        {
            Close();
        }
    }
}
