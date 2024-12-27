using NeeLaboratory.RealtimeSearch.Models;
using NeeLaboratory.RealtimeSearch.TextResource;
using System;
using System.Globalization;
using System.Windows;


namespace NeeLaboratory.RealtimeSearch
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {

        private AppModel? _appModel;

        public App()
        {
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                _appModel = new AppModel(AppSettings.Current);
                AppModel.Instance = _appModel;

                InitializeTextResource();
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
                MessageBox.Show(ex.Message, AppModel.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                _appModel?.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, AppModel.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }


        /// <summary>
        /// 言語リソース初期化
        /// </summary>
        private void InitializeTextResource()
        {
            CultureInfo culture;
            try
            {
                culture = CultureInfo.GetCultureInfo(AppModel.AppConfig.Language);
            }
            catch (CultureNotFoundException)
            {
                culture = CultureInfo.CurrentCulture;
            }
            TextResources.Initialize(culture);
            AppModel.AppConfig.Language = TextResources.Culture.Name;
            //InputGestureDisplayString.Initialize(TextResources.Resource);
        }
    }
}
