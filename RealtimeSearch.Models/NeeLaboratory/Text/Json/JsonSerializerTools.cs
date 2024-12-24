using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeeLaboratory.Text.Json
{
    public static class JsonSerializerTools
    {
        private static JsonSerializerOptions? _options;

        public static JsonSerializerOptions GetSerializerOptions()
        {
            return _options ??= CreateSerializerOptions();
        }

        private static JsonSerializerOptions CreateSerializerOptions()
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
                IgnoreReadOnlyProperties = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }
    }
}
