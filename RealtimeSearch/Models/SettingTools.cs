using System;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeeLaboratory.RealtimeSearch
{
    public static class SettingTools
    {
        public static string CreateSettingFileName()
        {
            return System.IO.Path.Combine(App.Config.LocalApplicationDataPath, App.Config.ProductName + ".app.json");
        }

        public static string CreateLegacySettingFileName()
        {
            return System.IO.Path.Combine(App.Config.LocalApplicationDataPath, "UserSetting.xml");
        }

        private static JsonSerializerOptions CreateJsonSerializerOptions()
        {
            var options = new JsonSerializerOptions();
            options.WriteIndented = true;
            options.IgnoreReadOnlyProperties = true;
            options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        private static void Save(Setting setting, string path)
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(setting, CreateJsonSerializerOptions());
            File.WriteAllBytes(path, json);
        }

        private static Setting? Load(string path)
        {
            if (!File.Exists(path)) return null;

            var json = File.ReadAllBytes(path);
            var readOnlySpan = new ReadOnlySpan<byte>(json);
            var setting = JsonSerializer.Deserialize<Setting>(readOnlySpan, CreateJsonSerializerOptions());
            return setting;
        }

        // TODO ASync
        public static Setting? Load()
        {
            try
            {
                var fileName = CreateSettingFileName();
                var setting = Load(fileName);

                // 互換処理
                if (setting is null)
                {
                    setting = LoadAndReplaceLegacy();
                }

                return setting;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                System.Windows.MessageBox.Show("設定が壊れています。", "読み込み失敗", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw;
            }
        }

        // TODO: ASync
        public static void Save(Setting setting)
        {
            var fileName = CreateSettingFileName();
            Save(setting, fileName);
        }

        private static Setting? LoadAndReplaceLegacy()
        {
            var newFileName = CreateSettingFileName();
            var oldFileName = CreateLegacySettingFileName();

            if (!System.IO.File.Exists(oldFileName)) return null;

#pragma warning disable CS0612 // 型またはメンバーが旧型式です
            var legacy = SettingLegacy.Load(oldFileName);
#pragma warning restore CS0612 // 型またはメンバーが旧型式です
            if (legacy is null) return null;

            var setting = legacy.ConvertToSetting();
            Save(setting, newFileName);

            File.Delete(oldFileName);

            return setting;
        }
    }
}
