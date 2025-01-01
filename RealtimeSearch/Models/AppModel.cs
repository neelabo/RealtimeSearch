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


        public static AppSettings Settings => Instance._settings;


        private readonly PersistAndRestoreService _persistAndRestoreService;
        private readonly AppSettings _settings;


        public AppModel()
        {
            _persistAndRestoreService = new PersistAndRestoreService(new FileService(), ApplicationInfo.Current);

            // カレントフォルダ設定
            //System.Environment.CurrentDirectory = Config.LocalApplicationDataPath;

            // 設定ファイル読み込み
            try
            {
                var settings = _persistAndRestoreService.Load();
                _settings = settings?.Validate() ?? new AppSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(CreateExceptionMessage(ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _settings = new AppSettings();
            }
        }


        public void Dispose()
        {
            // 設定ファイル保存
            _persistAndRestoreService.Save(_settings);
        }

        private static string CreateExceptionMessage(Exception ex)
        {
            if (ex.InnerException is not null)
            {
                return ex.Message + "\n" + CreateExceptionMessage(ex.InnerException);
            }
            else
            {
                return ex.Message;
            }
        }
    }
}
