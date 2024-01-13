using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace NeeLaboratory.RealtimeSearch
{
    public class ClipboardChangedEventArgs : EventArgs
    {
        public string Keyword { get; set; } = "";
    }

    public class ClipboardSearch
    {
        #region NativeMethods

        private static class NativeMethods
        {
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr GetForegroundWindow();
        }

        #endregion NativeMethods


        private ClipboardListener? _clipboardListener;
        private readonly AppConfig _setting;


        public ClipboardSearch(AppConfig setting)
        {
            _setting = setting;
        }


        public event EventHandler<ClipboardChangedEventArgs>? ClipboardChanged;


        public void Start(Window window)
        {
            // クリップボード監視
            _clipboardListener = new ClipboardListener(window);
            _clipboardListener.ClipboardUpdate += ClipboardListener_DrawClipboard;
        }

        public void Stop()
        {
            // クリップボード監視終了
            _clipboardListener?.Dispose();
            _clipboardListener = null;
        }


        public async void ClipboardListener_DrawClipboard(object? sender, Window window)
        {
            //var obj = Clipboard.GetDataObject();
            //Debug.WriteLine($"Capture: {string.Join(',', obj.GetFormats())}");

            // 自分のアプリからコピーした場合の変更は除外する
            IntPtr activeWindow = NativeMethods.GetForegroundWindow();
            IntPtr thisWindow = new WindowInteropHelper(window).Handle;
            if (activeWindow == thisWindow)
            {
                Debug.WriteLine("cannot use clipboard: window is active. (WIN32)");
                return;
            }

            // うまく動作しない時があるのでいったん無効化
#if false
            // Ctrl+V が押されているときはペースト動作に伴うクリップボード変更通知と判断し除外する
            if (Keyboard.Modifiers == ModifierKeys.Control && (Keyboard.GetKeyStates(Key.V) & (KeyStates.Down | KeyStates.Toggled)) != 0)
            {
                Debug.WriteLine("paste action maybe.");
                return;
            }
#endif

            // どうにも例外(CLIPBRD_E_CANT_OPEN)が発生してしまうのでリトライさせることにした
            for (int i = 0; i < 10; ++i)
            {
                try
                {
                    if (_setting.IsMonitorClipboard)
                    {
                        // text
                        if (Clipboard.ContainsText())
                        {
                            string text = Clipboard.GetText();

                            // 即時検索
                            ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs()
                            {
                                Keyword = new Regex(@"\s+").Replace(text, " ").Trim()
                            });
                        }
                        // file
                        else if (Clipboard.ContainsFileDropList())
                        {
                            var files = Clipboard.GetFileDropList();
                            var file = files[0];

                            var name = System.IO.Directory.Exists(file)
                                ? System.IO.Path.GetFileName(file)
                                : System.IO.Path.GetFileNameWithoutExtension(file);

                            // 即時検索
                            ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs()
                            {
                                Keyword = name ?? ""
                            });
                        }
                    }
                    return;
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    Debug.WriteLine(ex.Message);
                    await Task.Delay(100);
                }
            }
            throw new ApplicationException("クリップボードの参照に失敗しました。");
        }
    }
}
