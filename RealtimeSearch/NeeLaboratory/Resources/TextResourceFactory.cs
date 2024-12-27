using NeeLaboratory.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NeeLaboratory.Resources
{
    /// <summary>
    /// テキストリソース生成
    /// </summary>
    public class TextResourceFactory
    {
        private static readonly char[] _tokenSeparator = new char[] { '=' };
        private static readonly char[] _caseSeparator = new char[] { ':' };

        private readonly LanguageResource _languageResource;

        public TextResourceFactory(LanguageResource languageResource)
        {
            _languageResource = languageResource;
        }

        public TextResourceSet Load(CultureInfo culture)
        {
            var culture0 = _languageResource.DefaultCulture;
            var culture1 = culture;

            var res = LoadResText(culture0);

            if (!culture1.Equals(culture0))
            {
                var res1 = LoadResText(culture1);
                res1.ToList().ForEach(e => res[e.Key] = e.Value);
            }

            return new TextResourceSet(culture1, res);
        }

        private Dictionary<string, TextResourceItem> LoadResText(CultureInfo culture)
        {
            return LoadResText(_languageResource.CreateFileSource(culture));
        }

        public static Dictionary<string, TextResourceItem> LoadResText(IFileSource fileSource)
        {
            Stream stream;
            try
            {
                stream = fileSource.Open();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return new Dictionary<string, TextResourceItem>();
            }

            try
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);

                var dictionary = new Dictionary<string, TextResourceItem>();

                foreach (var pair in ReadLines(reader).Select(e => DeserializeResText(e)).Where(e => !string.IsNullOrEmpty(e.Key)))
                {
                    var tokens = pair.Key.Split(_caseSeparator, 2);
                    var key = tokens[0];
                    var regex = tokens.Length >= 2 ? CreateFullRegex(tokens[1]) : null;
                    var text = pair.Value;

                    if (!dictionary.TryGetValue(key, out var value))
                    {
                        value = new TextResourceItem();
                        dictionary.Add(key, value);
                    }

                    value.AddText(text, regex);
                }

                return dictionary;
            }
            finally
            {
                stream.Dispose();
            }
        }

        private static Regex CreateFullRegex(string s)
        {
            return new Regex("^" + s.TrimStart('^').TrimEnd('$') + "$");
        }

        private static IEnumerable<string> ReadLines(StreamReader reader)
        {
            while (true)
            {
                var s = reader.ReadLine();
                if (s is null) yield break;
                yield return s;
            }
        }

        private static (string Key, string Value) DeserializeResText(string s)
        {
            s = s.Trim();
            if (string.IsNullOrEmpty(s) || s[0] == ';' || s[0] == '#')
            {
                return ("", "");
            }
            var tokens = s.Split(_tokenSeparator, 2);
            if (tokens.Length != 2)
            {
                Debug.WriteLine($"ResText: FormatException");
                return ("", "");
            }
            var key = tokens[0].Trim();
            var body = tokens[1].Trim().Unescape();
            if (string.IsNullOrEmpty(body))
            {
                return ("", "");
            }

            return (key, body);
        }
    }
}
