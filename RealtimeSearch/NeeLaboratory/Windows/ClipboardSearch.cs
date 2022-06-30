using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace NeeLaboratory.RealtimeSearch
{
    public class ClipboardChangedEventArgs : EventArgs
    {
        public string Keyword { get; set; } = "";
    }

    //
    public class ClipboardSearch
    {
        private ClipboardListner? _clipboardListner;
        private AppConfig _setting;
        private string _copyText = "";


        public ClipboardSearch(AppConfig setting)
        {
            _setting = setting;
        }


        public event EventHandler<ClipboardChangedEventArgs>? ClipboardChanged;

        
        public void Start(Window window)
        {
            // クリップボード監視
            _clipboardListner = new ClipboardListner(window);
            _clipboardListner.ClipboardUpdate += ClipboardListner_DrawClipboard;
        }

        //
        public void Stop()
        {
            // クリップボード監視終了
            _clipboardListner?.Dispose();
            _clipboardListner = null;
        }


        public void SetClipboard(string text)
        {
            Clipboard.SetDataObject(text);
            _copyText = text;
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetForegroundWindow();
        
        public async void ClipboardListner_DrawClipboard(object? sender, Window window)
        {
            IntPtr activeWindow = GetForegroundWindow();
            IntPtr thisWindow = new WindowInteropHelper(window).Handle;
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
                    if (_setting.IsMonitorClipboard)
                    {
                        // text
                        if (Clipboard.ContainsText())
                        {
                            string text = Clipboard.GetText();
                            if (_copyText == text) return; // コピーしたファイル名と同じであるなら処理しない

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
