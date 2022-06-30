using System;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeeLaboratory.RealtimeSearch
{
    public class PersistAndRestoreService
    {
        private readonly IFileService _fileService;


        public PersistAndRestoreService(IFileService fileService)
        {
            _fileService = fileService;
        }


        public string FolderPath => App.AppInfo.LocalApplicationDataPath;

        public string AppConfigFileName => App.AppInfo.ProductName + ".app.json";

        public string LegacySettingFileName => "UserSetting.xml";


        // TODO ASync
        public AppConfig? Load()
        {
            try
            {
                var setting = _fileService.Read<AppConfig>(FolderPath, AppConfigFileName);

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
                throw new ApplicationException("設定の読み込みに失敗しました。", ex);
            }
        }

        // TODO: ASync
        public void Save(AppConfig appConfig)
        {
            try
            {
                _fileService.Write(FolderPath, AppConfigFileName, appConfig, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw new ApplicationException("設定の保存に失敗しました。", ex);
            }
        }

        private AppConfig? LoadAndReplaceLegacy()
        {
            var legacyFileName = Path.Combine(FolderPath, LegacySettingFileName);
            if (!File.Exists(legacyFileName)) return null;

#pragma warning disable CS0612 // 型またはメンバーが旧型式です
            var legacy = SettingLegacy.Load(legacyFileName);
#pragma warning restore CS0612 // 型またはメンバーが旧型式です
            if (legacy is null) return null;

            var appConfig = legacy.ConvertToAppConfig();
            Save(appConfig);

            File.Delete(legacyFileName);

            return appConfig;
        }
    }
}
