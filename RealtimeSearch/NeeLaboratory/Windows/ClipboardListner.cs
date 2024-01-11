﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace NeeLaboratory.RealtimeSearch
{
    public class ClipboardListener : IDisposable
    {
        private static class NativeMethods
        {
            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            internal extern static void AddClipboardFormatListener(IntPtr hwnd);

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            internal extern static void RemoveClipboardFormatListener(IntPtr hwnd);
        }

        //private const int WM_CLOSE = 0x0010;
        private const int WM_CLIPBOARDUPDATE = 0x031D;

        private readonly Window _window;
        private IntPtr _handle;
        private bool _disposedValue;


        public ClipboardListener(Window window)
        {
            _window = window;
            Open();
        }


        public event EventHandler<Window>? ClipboardUpdate;


        public void Open()
        {
            if (_handle != IntPtr.Zero) throw new ApplicationException("ClipboardListener is already opened.");

            _handle = new WindowInteropHelper(_window).Handle;

            NativeMethods.AddClipboardFormatListener(_handle);

            HwndSource source = HwndSource.FromHwnd(_handle);
            source.AddHook(new HwndSourceHook(WndProc));
        }


        public void Close()
        {
            if (_handle != IntPtr.Zero)
            {
                NativeMethods.RemoveClipboardFormatListener(_handle);
                _handle = IntPtr.Zero;
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

        private void NotifyClipboardUpdate()
        {
            if (ClipboardUpdate is null) return;

            _window.Dispatcher.BeginInvoke(new Action(() =>
            {
                ClipboardUpdate?.Invoke(this, _window);
            }));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                }
                Close();
                _disposedValue = true;
            }
        }

        ~ClipboardListener()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
