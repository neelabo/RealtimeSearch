using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace NeeLaboratory.Input
{
    public static class KeyExtensions
    {
        private static readonly Dictionary<Key, string> _map;
        private static StringConverter _displayStringConverter = StringConverter.Default;

        static KeyExtensions()
        {
            _map = KeyExConverter.DefaultKeyStringMap.ToDictionary(e => e.Key, e => e.Value);
        }

        public static void SetDisplayStringConverter(StringConverter converter)
        {
            _displayStringConverter = converter;
        }

        public static void SetDisplayString(this Key key, string value)
        {
            _map[key] = value;
        }

        public static string GetDisplayString(this Key key)
        {
            if (key == Key.None)
            {
                return "";
            }

            if (_map.TryGetValue(key, out var s))
            {
                return _displayStringConverter.Convert(s);
            }

            if (key >= Key.D0 && key <= Key.D9)
            {
                return char.ToString((char)(int)(key - Key.D0 + '0'));
            }

            if (key >= Key.A && key <= Key.Z)
            {
                return char.ToString((char)(int)(key - Key.A + 'A'));
            }

            if (KeyExConverter.IsDefinedKey(key))
            {
                return _displayStringConverter.Convert(key.ToString());
            }

            return "";
        }
    }

}
