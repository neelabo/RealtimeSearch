using System;
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
    public class ClipboardListner : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private extern static void AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private extern static void RemoveClipboardFormatListener(IntPtr hwnd);

        private const int WM_CLOSE = 0x0010;
        private const int WM_CLIPBOARDUPDATE = 0x031D;

        private Window _window;
        private IntPtr _handle;

        public event EventHandler<Window>? ClipboardUpdate;


        private void NotifyClipboardUpdate()
        {
            if (ClipboardUpdate != null)
            {
                _window.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ClipboardUpdate(this, _window);
                }));
            }
        }


        public ClipboardListner(Window window)
        {
            _window = window;
            Open();
        }


        public void Open()
        {
            if (_handle != IntPtr.Zero) throw new ApplicationException("ClipboardListner is already opened.");

            _handle = new WindowInteropHelper(_window).Handle;

            AddClipboardFormatListener(_handle);

            HwndSource source = HwndSource.FromHwnd(_handle);
            source.AddHook(new HwndSourceHook(WndProc));
        }


        public void Close()
        {
            if (_handle != IntPtr.Zero)
            {
                RemoveClipboardFormatListener(_handle);
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


        public void Dispose()
        {
            Close();
        }
    }
}
