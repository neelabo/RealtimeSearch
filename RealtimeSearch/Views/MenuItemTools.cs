using System;
using System.Globalization;

namespace NeeLaboratory.RealtimeSearch.Views
{
    public static class MenuItemTools
    { 
        public static string IntegerToAccessKey(int value)
        {
            var s = value.ToString(CultureInfo.InvariantCulture);
            if (s.Length == 1)
            {
                return "_" + s;
            }
            else
            {
                return s[0..^1] + "_" + s[^1];
            }
        }

        public static string EscapeMenuItemString(string source)
        {
            return source.Replace("_", "__", StringComparison.Ordinal);
        }
    }


}
