using System.Runtime.InteropServices;

namespace NeeLaboratory.Native
{
    internal static partial class NativeMethods
    {
        // 参考：自然順ソート
        [LibraryImport("shlwapi.dll", EntryPoint = "StrCmpLogicalW", StringMarshalling = StringMarshalling.Utf16)]
        public static partial int StrCmpLogicalW(string psz1, string psz2);
    }

}
