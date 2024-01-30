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
        public static AppModel Instance { get; private set; } = new AppModel();

        public static AppConfig AppConfig => Instance._appConfig;

        public static ApplicationInfoService AppInfo => Instance._appInfo;


        private readonly PersistAndRestoreService _persistAndRestoreService;
        private readonly ApplicationInfoService _appInfo;
        private AppConfig _appConfig = new AppConfig();


        private AppModel()
        {
            _appInfo = new ApplicationInfoService();
            _persistAndRestoreService = new PersistAndRestoreService(new FileService(), _appInfo);

            _appInfo.Initialize();

            try
            {
                // カレントフォルダ設定
                //System.Environment.CurrentDirectory = Config.LocalApplicationDataPath;

                // 設定ファイル読み込み
                var appConfig = _persistAndRestoreService.Load();
                appConfig?.Validate();
                if (appConfig != null)
                {
                    _appConfig = appConfig;
                }

#if false
                // logger
                //var sw = new StreamWriter("TraceLog.txt");
                //sw.AutoFlush = true;
                //var tw = TextWriter.Synchronized(sw);
                //var twtl = new TextWriterTraceListener(tw, Development.Logger.TraceSource.Name);
                //Trace.Listeners.Add(twtl);

                var appTrace = Development.Logger.TraceSource;
                appTrace.Listeners.Remove("Default");
                var twtl = new TextWriterTraceListener("TraceLog.txt", "LogFile");
                appTrace.Listeners.Add(twtl);
                Development.Logger.SetLevel(SourceLevels.All);
                Development.Logger.Trace(System.Environment.NewLine + new string('=', 80));

                //Development.Logger.SetLevel(SourceLevels.All);
                //var twtl = new TextWriterTraceListener("TraceLog.txt", Development.Logger.TraceSource.Name);
                //Trace.Listeners.Add(twtl);
                //Trace.AutoFlush = true;
                //Trace.WriteLine(System.Environment.NewLine + new string('=', 80));
#endif
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _appInfo.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }


        public void Dispose()
        {
            try
            {
                // 設定ファイル保存
                _persistAndRestoreService.Save(_appConfig);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, _appInfo.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
    }
}
