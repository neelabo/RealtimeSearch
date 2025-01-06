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
using NeeLaboratory.RealtimeSearch.Models;

namespace NeeLaboratory.RealtimeSearch.Clipboards
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
            public static extern nint GetForegroundWindow();
        }

        #endregion NativeMethods


        private ClipboardListener? _clipboardListener;
        private readonly AppSettings _setting;
        private string? _copyText;


        public ClipboardSearch(AppSettings setting)
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

            if (!_setting.MonitorClipboard) return;

            // 自分のアプリからコピーした場合の変更は除外する
            nint activeWindow = NativeMethods.GetForegroundWindow();
            nint thisWindow = new WindowInteropHelper(window).Handle;
            if (activeWindow == thisWindow)
            {
                Debug.WriteLine("cannot use clipboard: window is active. (WIN32)");
                return;
            }

            // どうにも例外(CLIPBRD_E_CANT_OPEN)が発生してしまうのでリトライさせることにした
            for (int i = 0; i < 10; ++i)
            {
                try
                {
                    // text
                    if (Clipboard.ContainsText())
                    {
                        string text = Clipboard.GetText();

                        // 自アプリからコピーしたテキストは無視す
                        if (_copyText == text)
                        {
                            Debug.WriteLine($"Clipboard: same text. {text}");
                            return;
                        }
                        else
                        {
                            _copyText = null;
                        }

                        // 即時検索
                        ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs()
                        {
                            Keyword = new Regex(@"\s+").Replace(text, " ").Trim()
                        });
                    }
                    // file
                    else if (Clipboard.ContainsFileDropList())
                    {
                        _copyText = null;

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
                    else
                    {
                        _copyText = null;
                    }
                    return;
                }
                catch (COMException ex)
                {
                    Debug.WriteLine(ex.Message);
                    await Task.Delay(100);
                }
            }
            throw new ApplicationException("クリップボードの参照に失敗しました。");
        }

        public void ResetClipboardText()
        {
            _copyText = null;
        }

        public void SetTextToClipboard(string text)
        {
            _copyText = text;
            Clipboard.SetText(text);
        }

        public void SetFileDropListToClipboard(string[] files)
        {
            _copyText = null;
            var fileDropList = new System.Collections.Specialized.StringCollection();
            fileDropList.AddRange(files);
            Clipboard.SetFileDropList(fileDropList);
        }

    }
}
