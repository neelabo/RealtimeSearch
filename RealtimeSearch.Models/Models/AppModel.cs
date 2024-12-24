using NeeLaboratory.RealtimeSearch.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NeeLaboratory.RealtimeSearch.Models
{

    public class AppModel : IDisposable
    {
        public static string ApplicationName => "RealtimeSearch";


        private static AppModel? _instance;
        public static AppModel Instance
        {
            get => _instance ?? throw new InvalidOperationException("Instance does not exist.");
            set => _instance = value;
        }


        public static AppConfig AppConfig => Instance._appConfig;

        public static ApplicationInfoService AppInfo => Instance._appInfo;


        private readonly PersistAndRestoreService _persistAndRestoreService;
        private readonly ApplicationInfoService _appInfo;
        private AppConfig _appConfig = new AppConfig();


        public AppModel(IAppSettings appSetting)
        {
            _appInfo = new ApplicationInfoService(appSetting);
            _appInfo.Initialize();

            _persistAndRestoreService = new PersistAndRestoreService(new FileService(), _appInfo);

            // カレントフォルダ設定
            //System.Environment.CurrentDirectory = Config.LocalApplicationDataPath;

            // 設定ファイル読み込み
            var appConfig = _persistAndRestoreService.Load();
            appConfig?.Validate();
            if (appConfig != null)
            {
                _appConfig = appConfig;
            }
        }


        public void Dispose()
        {
            // 設定ファイル保存
            _persistAndRestoreService.Save(_appConfig);
        }
    }
}
