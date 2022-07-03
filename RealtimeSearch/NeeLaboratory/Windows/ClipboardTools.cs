using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace NeeLaboratory.RealtimeSearch
{
    public static class ClipboardTools
    {
        public static void SetText(string text)
        {
            Clipboard.SetText(text);
        }

        public static void SetFileDropList(IEnumerable<string> files)
        {
            var fileDropList = new System.Collections.Specialized.StringCollection();
            fileDropList.AddRange(files.ToArray());
            Clipboard.SetFileDropList(fileDropList);
        }
    }
}
